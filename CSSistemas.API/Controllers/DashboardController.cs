using CSSistemas.Application.DTOs.Dashboard;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Helpers;
using CSSistemas.API.Extensions;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private static readonly TimeZoneInfo BrazilTz = BrazilTimeHelper.GetBrazilTimeZone();
    private static readonly string[] MonthLabels = { "Jan", "Fev", "Mar", "Abr", "Mai", "Jun", "Jul", "Ago", "Set", "Out", "Nov", "Dez" };

    private readonly IBusinessRepository _businessRepository;
    private readonly IAppointmentRepository _appointmentRepository;

    public DashboardController(IBusinessRepository businessRepository, IAppointmentRepository appointmentRepository)
    {
        _businessRepository = businessRepository;
        _appointmentRepository = appointmentRepository;
    }

    /// <summary>Resumo do dashboard para um negócio (cards, agenda do dia, próximos, ganhos do mês).</summary>
    [HttpGet("summary")]
    [ProducesResponseType(typeof(DashboardSummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary([FromQuery] Guid businessId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");

        var now = DateTime.UtcNow;
        // "Hoje" no fuso Brasil para bater com a aba Agendamentos e contar todas as faltas do dia
        var hojeBrasil = TimeZoneInfo.ConvertTimeFromUtc(now, BrazilTz);
        var todayStartBrasil = new DateTime(hojeBrasil.Year, hojeBrasil.Month, hojeBrasil.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var todayEndBrasil = todayStartBrasil.AddDays(1).AddTicks(-1);
        var todayStart = TimeZoneInfo.ConvertTimeToUtc(todayStartBrasil, BrazilTz);
        var todayEnd = TimeZoneInfo.ConvertTimeToUtc(todayEndBrasil, BrazilTz);
        // Mês atual no fuso Brasil (para ganhos do mês bater com o que o usuário entende)
        var monthStartBrasil = new DateTime(hojeBrasil.Year, hojeBrasil.Month, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var monthEndBrasil = monthStartBrasil.AddMonths(1).AddTicks(-1);
        var monthStart = TimeZoneInfo.ConvertTimeToUtc(monthStartBrasil, BrazilTz);
        var monthEnd = TimeZoneInfo.ConvertTimeToUtc(monthEndBrasil, BrazilTz);
        var next30End = now.AddDays(30);

        var appointmentsToday = await _appointmentRepository.GetByBusinessIdWithServiceAsync(businessId, todayStart, todayEnd, cancellationToken);
        var appointmentsMonth = await _appointmentRepository.GetByBusinessIdWithServiceAsync(businessId, monthStart, monthEnd, cancellationToken);
        var appointmentsNext = await _appointmentRepository.GetByBusinessIdWithServiceAsync(businessId, now, next30End, cancellationToken);

        var confirmadosOuPendentesHoje = appointmentsToday.Where(a => a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Pending).ToList();
        var faltas = appointmentsToday.Count(a => a.Status == AppointmentStatus.Cancelled);
        var ganhosDoMes = appointmentsMonth
            .Where(a => a.Status == AppointmentStatus.Completed && a.Service.Price.HasValue)
            .Sum(a => a.Service.Price!.Value);

        var agendaDoDia = confirmadosOuPendentesHoje
            .OrderBy(a => a.ScheduledAt)
            .Select(a => new AgendaItemDto(
                TimeZoneInfo.ConvertTimeFromUtc(a.ScheduledAt, BrazilTz).ToString("HH:mm"),
                a.Service.Name,
                a.ClientName))
            .ToList();

        var proximos = appointmentsNext
            .Where(a => a.Status == AppointmentStatus.Confirmed || a.Status == AppointmentStatus.Pending)
            .OrderBy(a => a.ScheduledAt)
            .Take(10)
            .ToList();

        var proximosDtos = new List<ProximoAgendamentoDto>();
        foreach (var a in proximos)
        {
            var aBrasil = TimeZoneInfo.ConvertTimeFromUtc(a.ScheduledAt, BrazilTz);
            var dataStr = aBrasil.Date == hojeBrasil.Date ? "Hoje" : aBrasil.Date == hojeBrasil.Date.AddDays(1) ? "Amanhã" : aBrasil.ToString("dd/MM");
            proximosDtos.Add(new ProximoAgendamentoDto(dataStr, aBrasil.ToString("HH:mm"), a.ClientName, a.Service.Name));
        }

        var response = new DashboardSummaryResponse(
            ProximosAgendamentosCount: proximos.Count,
            ClientesHojeCount: confirmadosOuPendentesHoje.Count,
            FaltasCount: faltas,
            GanhosDoMes: ganhosDoMes,
            AgendaDoDia: agendaDoDia,
            ProximosAgendamentos: proximosDtos);

        return Ok(response);
    }

    /// <summary>Ganhos por mês (últimos N meses) para gráfico na página Ganhos.</summary>
    [HttpGet("earnings")]
    [ProducesResponseType(typeof(EarningsByMonthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEarnings([FromQuery] Guid businessId, [FromQuery] int months = 12, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");

        if (months < 1 || months > 24) months = 12;
        var now = DateTime.UtcNow;
        var hojeBrasil = TimeZoneInfo.ConvertTimeFromUtc(now, BrazilTz);
        var startBrasil = new DateTime(hojeBrasil.Year, hojeBrasil.Month, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(-(months - 1));
        var start = TimeZoneInfo.ConvertTimeToUtc(startBrasil, BrazilTz);
        var end = now;

        var appointments = await _appointmentRepository.GetByBusinessIdWithServiceAsync(businessId, start, end, cancellationToken);
        var completedWithPrice = appointments
            .Where(a => a.Status == AppointmentStatus.Completed && a.Service.Price.HasValue)
            .ToList();

        var byMonth = new List<EarningsByMonthDto>();
        for (var i = 0; i < months; i++)
        {
            var monthStartBrasil = startBrasil.AddMonths(i);
            var monthEndBrasil = monthStartBrasil.AddMonths(1).AddTicks(-1);
            var monthStart = TimeZoneInfo.ConvertTimeToUtc(monthStartBrasil, BrazilTz);
            var monthEnd = TimeZoneInfo.ConvertTimeToUtc(monthEndBrasil, BrazilTz);

            var total = completedWithPrice
                .Where(a => a.ScheduledAt >= monthStart && a.ScheduledAt <= monthEnd)
                .Sum(a => a.Service.Price!.Value);

            byMonth.Add(new EarningsByMonthDto(
                monthStartBrasil.Year,
                monthStartBrasil.Month,
                MonthLabels[monthStartBrasil.Month - 1],
                total));
        }

        return Ok(new EarningsByMonthResponse(byMonth));
    }

    /// <summary>Detalhe dos ganhos: lista de agendamentos concluídos com serviço e valor. Aceita período (from/to) ou mês (year/month).</summary>
    [HttpGet("earnings-detail")]
    [ProducesResponseType(typeof(EarningsDetailResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEarningsDetail(
        [FromQuery] Guid businessId,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int? year = null,
        [FromQuery] int? month = null,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");

        DateTime rangeStartUtc;
        DateTime rangeEndUtc;

        if (from.HasValue && to.HasValue && from.Value.Date <= to.Value.Date)
        {
            var fromBrasil = DateTime.SpecifyKind(from.Value.Date, DateTimeKind.Unspecified);
            var toBrasil = DateTime.SpecifyKind(to.Value.Date.AddHours(23).AddMinutes(59).AddSeconds(59), DateTimeKind.Unspecified);
            rangeStartUtc = TimeZoneInfo.ConvertTimeToUtc(fromBrasil, BrazilTz);
            rangeEndUtc = TimeZoneInfo.ConvertTimeToUtc(toBrasil, BrazilTz);
        }
        else
        {
            var hojeBrasil = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, BrazilTz);
            var yearUse = year ?? hojeBrasil.Year;
            var monthUse = month ?? hojeBrasil.Month;
            var monthStartBrasil = new DateTime(yearUse, monthUse, 1, 0, 0, 0, DateTimeKind.Unspecified);
            var monthEndBrasil = monthStartBrasil.AddMonths(1).AddTicks(-1);
            rangeStartUtc = TimeZoneInfo.ConvertTimeToUtc(monthStartBrasil, BrazilTz);
            rangeEndUtc = TimeZoneInfo.ConvertTimeToUtc(monthEndBrasil, BrazilTz);
        }

        var appointments = await _appointmentRepository.GetByBusinessIdWithServiceAsync(businessId, rangeStartUtc, rangeEndUtc, cancellationToken);
        var items = appointments
            .Where(a => a.Status == AppointmentStatus.Completed && a.Service.Price.HasValue)
            .OrderBy(a => a.ScheduledAt)
            .Select(a => new EarningsDetailItemDto(a.ScheduledAt, a.ClientName, a.Service.Name, a.Service.Price!.Value))
            .ToList();

        return Ok(new EarningsDetailResponse(items));
    }
}
