using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.PublicBooking;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Helpers;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

/// <summary>API pública de agendamento (sem autenticação). Acesso via link do negócio (publicSlug).</summary>
[ApiController]
[Route("api/public/booking")]
[AllowAnonymous]
public class PublicBookingController : ControllerBase
{
    private readonly IBusinessRepository _businessRepo;
    private readonly IServiceRepository _serviceRepo;
    private readonly IAppointmentRepository _appointmentRepo;
    private readonly INotificationRepository _notificationRepo;
    private readonly IAvailabilityService _availabilityService;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;
    private readonly IValidator<CreatePublicAppointmentRequest> _validator;
    private readonly ILogger<PublicBookingController> _logger;

    public PublicBookingController(
        IBusinessRepository businessRepo,
        IServiceRepository serviceRepo,
        IAppointmentRepository appointmentRepo,
        INotificationRepository notificationRepo,
        IAvailabilityService availabilityService,
        IEmailSender emailSender,
        IConfiguration config,
        IValidator<CreatePublicAppointmentRequest> validator,
        ILogger<PublicBookingController> logger)
    {
        _businessRepo = businessRepo;
        _serviceRepo = serviceRepo;
        _appointmentRepo = appointmentRepo;
        _notificationRepo = notificationRepo;
        _availabilityService = availabilityService;
        _emailSender = emailSender;
        _config = config;
        _validator = validator;
        _logger = logger;
    }

    /// <summary>Obtém negócio pelo slug público (para exibir nome e serviços).</summary>
    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(PublicBusinessDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken = default)
    {
        var business = await _businessRepo.GetByPublicSlugAsync(slug, cancellationToken);
        if (business == null) throw CommException.NotFound("Link de agendamento não encontrado.");
        if (string.IsNullOrEmpty(business.PublicSlug)) throw CommException.NotFound("Agendamento público não disponível para este negócio.");
        return Ok(new PublicBusinessDto(business.Id, business.Name, business.PublicSlug));
    }

    /// <summary>Lista serviços do negócio (público).</summary>
    [HttpGet("{slug}/services")]
    [ProducesResponseType(typeof(IReadOnlyList<PublicServiceDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetServices(string slug, CancellationToken cancellationToken = default)
    {
        var business = await _businessRepo.GetByPublicSlugAsync(slug, cancellationToken);
        if (business == null) throw CommException.NotFound("Link de agendamento não encontrado.");
        var services = await _serviceRepo.GetByBusinessIdAsync(business.Id, onlyActive: true, cancellationToken);
        return Ok(services.Select(s => new PublicServiceDto(s.Id, s.Name, s.DurationMinutes, s.Price)));
    }

    /// <summary>Horários do dia com indicação disponível/ocupado. date = yyyy-MM-dd (dia no fuso Brasil). Ocupados podem ser exibidos em vermelho na UI.</summary>
    [HttpGet("{slug}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotWithAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlots(string slug, [FromQuery] Guid serviceId, [FromQuery] DateTime date, CancellationToken cancellationToken = default)
    {
        var business = await _businessRepo.GetByPublicSlugAsync(slug, cancellationToken);
        if (business == null) throw CommException.NotFound("Link de agendamento não encontrado.");
        var dateOnly = date.Date;
        var slots = await _availabilityService.GetSlotsWithAvailabilityAsync(business.Id, serviceId, dateOnly, 30, cancellationToken);
        return Ok(slots);
    }

    /// <summary>Cria agendamento via link público (sem login).</summary>
    [HttpPost("{slug}/appointments")]
    [ProducesResponseType(typeof(AppointmentCreatedDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateAppointment(string slug, [FromBody] CreatePublicAppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var business = await _businessRepo.GetByPublicSlugAsync(slug, cancellationToken);
        if (business == null) throw CommException.NotFound("Link de agendamento não encontrado.");
        if (string.IsNullOrEmpty(business.PublicSlug)) throw CommException.NotFound("Agendamento público não disponível.");

        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());

        var service = await _serviceRepo.GetByIdAndBusinessIdAsync(request.ServiceId, business.Id, cancellationToken);
        if (service == null) throw CommException.NotFound("Serviço não encontrado.");
        if (!service.IsActive) throw CommException.BadRequest("Serviço não está disponível.");

        // Sempre gravar em UTC. Se o cliente enviar sem "Z" (Unspecified), tratar como horário de Brasília e converter para UTC (evita 14:30 Brasil ser salvo como 14:30 UTC).
        var scheduledAt = ToUtcFromRequest(request.ScheduledAt);
        var hasConflict = await _appointmentRepo.HasConflictAsync(business.Id, scheduledAt, service.DurationMinutes, null, cancellationToken);
        if (hasConflict) throw CommException.Conflict("Este horário não está mais disponível. Escolha outro.");

        var appointment = Appointment.Create(
            business.Id, request.ServiceId, request.ClientName, scheduledAt,
            request.ClientPhone, request.ClientEmail, request.Notes);
        appointment.SetStatus(AppointmentStatus.Confirmed); // Cliente final agendou = confirmado em Agendamentos
        appointment.SetCancelToken(Guid.NewGuid().ToString("N"));
        await _appointmentRepo.AddAsync(appointment, cancellationToken);

        var notification = Notification.CreateNewAppointment(business.UserId, request.ClientName, scheduledAt, appointment.Id);
        await _notificationRepo.AddAsync(notification, cancellationToken);

        var baseUrl = _config["Email:PasswordResetBaseUrl"]?.Trim() ?? "";
        if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(request.ClientEmail))
        {
            var cancelLink = $"{baseUrl.TrimEnd('/')}/agendar/cancelar?token={Uri.EscapeDataString(appointment.CancelToken!)}";
            var scheduledFormatted = BrazilTimeHelper.FormatUtcToBrazilDateTime(scheduledAt);
            await _emailSender.SendAppointmentConfirmationAsync(
                request.ClientEmail, request.ClientName, scheduledFormatted, service.Name, business.Name, cancelLink, cancellationToken);
        }

        return CreatedAtAction(nameof(GetBySlug), new { slug }, new AppointmentCreatedDto(
            appointment.Id, appointment.BusinessId, appointment.ServiceId, appointment.ClientName,
            appointment.ScheduledAt, (int)appointment.Status));
    }

    /// <summary>Cancela agendamento pelo token enviado no e-mail de confirmação. Profissional recebe notificação no sistema. Validações apenas no backend. Erros de regra de negócio via CommException.</summary>
    [HttpPost("cancelar")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CancelByToken([FromQuery] string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw CommException.NotFound("Link de cancelamento inválido.");

        var appointment = await _appointmentRepo.GetByCancelTokenAsync(token.Trim(), cancellationToken);
        if (appointment == null)
            throw CommException.NotFound("Agendamento não encontrado ou já foi cancelado.");

        var business = await _businessRepo.GetByIdAsync(appointment.BusinessId, cancellationToken);
        if (business == null)
            throw CommException.NotFound("Agendamento não encontrado.");

        var deleted = await _appointmentRepo.SoftDeleteAsync(appointment.Id, appointment.BusinessId, cancellationToken);
        if (!deleted)
            throw CommException.NotFound("Agendamento não encontrado ou já foi cancelado.");

        try
        {
            var notification = Notification.CreateAppointmentCancelledByClient(business.UserId, appointment.ClientName, appointment.ScheduledAt, appointment.Id);
            await _notificationRepo.AddAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notificação de cancelamento pelo cliente não foi criada. Agendamento já cancelado. AppointmentId: {Id}", appointment.Id);
        }

        return Ok(new { message = "Agendamento cancelado com sucesso." });
    }

    /// <summary>Converte o horário do request para UTC. Se já for UTC (ex.: ISO com Z), mantém; se Unspecified, trata como Brasília e converte.</summary>
    private static DateTime ToUtcFromRequest(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value : BrazilTimeHelper.ConvertBrazilToUtc(value);

}

/// <summary>Resposta ao criar agendamento público.</summary>
public record AppointmentCreatedDto(
    Guid Id, Guid BusinessId, Guid ServiceId, string ClientName,
    DateTime ScheduledAt, int Status);
