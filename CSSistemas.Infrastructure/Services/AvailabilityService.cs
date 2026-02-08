using CSSistemas.Application.DTOs.PublicBooking;
using CSSistemas.Application.Helpers;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
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
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        IBusinessHoursRepository hoursRepo,
        IServiceRepository serviceRepo,
        IAppointmentRepository appointmentRepo,
        ILogger<AvailabilityService> logger)
    {
        _hoursRepo = hoursRepo;
        _serviceRepo = serviceRepo;
        _appointmentRepo = appointmentRepo;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(Guid businessId, Guid serviceId, DateTime date, int slotIntervalMinutes = 30, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepo.GetByIdAndBusinessIdAsync(serviceId, businessId, cancellationToken);
        if (service == null || !service.IsActive)
            return Array.Empty<DateTime>();

        // Sempre tratar a data como dia civil no fuso Brasil (evita bug quando o servidor está em outro fuso)
        var dateOnly = new DateOnly(date.Year, date.Month, date.Day);
        var dayOfWeek = (int)dateOnly.DayOfWeek; // 0=Sunday, 1=Monday, ..., 6=Saturday
        var hoursList = await _hoursRepo.GetByBusinessIdAsync(businessId, cancellationToken);
        var dayHours = hoursList.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

        int openMin, closeMin;
        if (dayHours != null)
        {
            openMin = dayHours.OpenAtMinutes;
            closeMin = dayHours.CloseAtMinutes;
        }
        else
        {
            // Default: Segunda a Sexta 8h-18h
            if (dayOfWeek == 0 || dayOfWeek == 6)
                return Array.Empty<DateTime>();
            openMin = DefaultOpenMinutes;
            closeMin = DefaultCloseMinutes;
        }

        var duration = service.DurationMinutes;
        if (closeMin - openMin < duration)
            return Array.Empty<DateTime>();

        var slots = new List<DateTime>();

        for (var minute = openMin; minute + duration <= closeMin; minute += slotIntervalMinutes)
        {
            // Horário de início no Brasil (Unspecified) e converter para UTC
            var localStart = dateOnly.ToDateTime(new TimeOnly(minute / 60, minute % 60));
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, BrazilTz);

            // Não oferecer horários no passado (em UTC)
            if (utcStart < DateTime.UtcNow)
                continue;

            var hasConflict = await _appointmentRepo.HasConflictAsync(businessId, utcStart, duration, null, cancellationToken);
            if (!hasConflict)
                slots.Add(utcStart);
        }

        return slots;
    }

    public async Task<IReadOnlyList<SlotWithAvailabilityDto>> GetSlotsWithAvailabilityAsync(Guid businessId, Guid serviceId, DateTime date, int slotIntervalMinutes = 30, CancellationToken cancellationToken = default)
    {
        var service = await _serviceRepo.GetByIdAndBusinessIdAsync(serviceId, businessId, cancellationToken);
        if (service == null || !service.IsActive)
            return Array.Empty<SlotWithAvailabilityDto>();

        var dateOnly = new DateOnly(date.Year, date.Month, date.Day);
        var dayOfWeek = (int)dateOnly.DayOfWeek;
        var hoursList = await _hoursRepo.GetByBusinessIdAsync(businessId, cancellationToken);
        var dayHours = hoursList.FirstOrDefault(h => h.DayOfWeek == dayOfWeek);

        int openMin, closeMin;
        if (dayHours != null)
        {
            openMin = dayHours.OpenAtMinutes;
            closeMin = dayHours.CloseAtMinutes;
        }
        else
        {
            if (dayOfWeek == 0 || dayOfWeek == 6)
                return Array.Empty<SlotWithAvailabilityDto>();
            openMin = DefaultOpenMinutes;
            closeMin = DefaultCloseMinutes;
        }

        var duration = service.DurationMinutes;
        if (closeMin - openMin < duration)
            return Array.Empty<SlotWithAvailabilityDto>();

        var result = new List<SlotWithAvailabilityDto>();

        for (var minute = openMin; minute + duration <= closeMin; minute += slotIntervalMinutes)
        {
            var localStart = dateOnly.ToDateTime(new TimeOnly(minute / 60, minute % 60));
            var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart, BrazilTz);

            if (utcStart < DateTime.UtcNow)
                continue;

            var hasConflict = await _appointmentRepo.HasConflictAsync(businessId, utcStart, duration, null, cancellationToken);
            result.Add(new SlotWithAvailabilityDto(utcStart, !hasConflict));
        }

        return result;
    }
}
