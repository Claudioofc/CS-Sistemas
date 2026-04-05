using CSSistemas.Application.DTOs.PublicBooking;
using CSSistemas.Application.Helpers;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Calcula horários disponíveis para agendamento. Assume fuso Brasil (UTC-3) para a data.</summary>
public class AvailabilityService : IAvailabilityService
{
    private static readonly TimeZoneInfo BrazilTz = BrazilTimeHelper.GetBrazilTimeZone();
    private const int DefaultOpenMinutes = 480;  // 08:00
    private const int DefaultCloseMinutes = 1080; // 18:00

    private readonly IBusinessHoursRepository _hoursRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly IEmployeeRepository _employeeRepo;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        IBusinessHoursRepository hoursRepo,
        IServiceRepository serviceRepo,
        IAppointmentRepository appointmentRepo,
        IEmployeeRepository employeeRepo,
        ILogger<AvailabilityService> logger)
    {
        _hoursRepo = hoursRepo;
        _serviceRepo = serviceRepo;
        _appointmentRepo = appointmentRepo;
        _employeeRepo = employeeRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(Guid businessId, Guid serviceId, DateTime date, int slotIntervalMinutes = 30, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepo.GetByIdAndBusinessIdAsync(serviceId, businessId, cancellationToken);
        if (service == null || !service.IsActive)
            return Array.Empty<DateTime>();

        var (openMin, closeMin, dateOnly) = await GetHoursAsync(businessId, date, cancellationToken);
        if (openMin < 0) return Array.Empty<DateTime>();

        var duration = service.DurationMinutes;
        if (closeMin - openMin < duration)
            return Array.Empty<DateTime>();

        // Carrega todos os agendamentos ativos do dia uma única vez (evita N+1)
        var dayAppointments = await LoadDayAppointmentsAsync(businessId, dateOnly, cancellationToken);

        var slots = new List<DateTime>();
        for (var minute = openMin; minute + duration <= closeMin; minute += slotIntervalMinutes)
        {
            var localStart = dateOnly.ToDateTime(new TimeOnly(minute / 60, minute % 60));
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, BrazilTz);

            if (utcStart < DateTime.UtcNow)
                continue;

            if (!HasConflictInMemory(dayAppointments, utcStart, duration, null, null))
                slots.Add(utcStart);
        }

        return slots;
    }

    public async Task<IReadOnlyList<SlotWithAvailabilityDto>> GetSlotsWithAvailabilityAsync(Guid businessId, Guid serviceId, DateTime date, int slotIntervalMinutes = 30, Guid? employeeId = null, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepo.GetByIdAndBusinessIdAsync(serviceId, businessId, cancellationToken);
        if (service == null || !service.IsActive)
            return Array.Empty<SlotWithAvailabilityDto>();

        var (openMin, closeMin, dateOnly) = await GetHoursAsync(businessId, date, cancellationToken);
        if (openMin < 0) return Array.Empty<SlotWithAvailabilityDto>();

        var duration = service.DurationMinutes;
        if (closeMin - openMin < duration)
            return Array.Empty<SlotWithAvailabilityDto>();

        // Capacidade: se não filtrou por funcionário, considera quantos funcionários ativos o negócio tem
        int? capacity = null;
        if (!employeeId.HasValue)
        {
            var activeCount = await _employeeRepo.CountActiveByBusinessIdAsync(businessId, cancellationToken);
            if (activeCount > 0)
                capacity = activeCount;
        }

        // Carrega todos os agendamentos ativos do dia uma única vez (evita N+1)
        var dayAppointments = await LoadDayAppointmentsAsync(businessId, dateOnly, cancellationToken);

        var result = new List<SlotWithAvailabilityDto>();
        for (var minute = openMin; minute + duration <= closeMin; minute += slotIntervalMinutes)
        {
            var localStart = dateOnly.ToDateTime(new TimeOnly(minute / 60, minute % 60));
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, BrazilTz);

            if (utcStart < DateTime.UtcNow)
                continue;

            var hasConflict = HasConflictInMemory(dayAppointments, utcStart, duration, employeeId, capacity);
            result.Add(new SlotWithAvailabilityDto(utcStart, !hasConflict));
        }

        return result;
    }

    // -------------------------------------------------------------------------

    private async Task<(int OpenMin, int CloseMin, DateOnly DateOnly)> GetHoursAsync(Guid businessId, DateTime date, CancellationToken cancellationToken)
    {
        var dateOnly = new DateOnly(date.Year, date.Month, date.Day);
        var dayOfWeek = (int)dateOnly.DayOfWeek;
        var hoursList = await _hoursRepo.GetByBusinessIdAsync(businessId, cancellationToken);
        var dayHours = hoursList.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

        if (dayHours != null)
            return (dayHours.OpenAtMinutes, dayHours.CloseAtMinutes, dateOnly);

        // Default: Segunda a Sexta 8h-18h; fora disso fechado
        if (dayOfWeek == 0 || dayOfWeek == 6)
            return (-1, -1, dateOnly);

        return (DefaultOpenMinutes, DefaultCloseMinutes, dateOnly);
    }

    private async Task<IReadOnlyList<Appointment>> LoadDayAppointmentsAsync(Guid businessId, DateOnly dateOnly, CancellationToken cancellationToken)
    {
        // Janela em UTC cobrindo todo o dia civil no Brasil (UTC-3 = +3h de margem)
        var dayStartLocal = dateOnly.ToDateTime(TimeOnly.MinValue);
        var dayEndLocal = dateOnly.ToDateTime(TimeOnly.MaxValue);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(dayStartLocal, BrazilTz);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(dayEndLocal, BrazilTz);

        return await _appointmentRepo.GetByBusinessIdWithServiceAsync(
            businessId, dayStartUtc, dayEndUtc, cancellationToken);
    }

    /// <summary>Verifica conflito de horário em memória (sem query ao banco). Requer appointments pré-carregados com Service.</summary>
    private static bool HasConflictInMemory(
        IReadOnlyList<Appointment> appointments,
        DateTime utcStart,
        int durationMinutes,
        Guid? employeeId,
        int? capacity)
    {
        var utcEnd = utcStart.AddMinutes(durationMinutes);

        var overlapping = appointments
            .Where(a => a.Status != AppointmentStatus.Cancelled
                     && a.ScheduledAt < utcEnd
                     && a.ScheduledAt.AddMinutes(a.Service!.DurationMinutes) > utcStart)
            .ToList();

        if (employeeId.HasValue)
            return overlapping.Any(a => a.EmployeeId == employeeId.Value);

        var cap = capacity ?? 1;
        return overlapping.Count >= cap;
    }
}
