using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.Employee;
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
    private readonly IEmployeeRepository _employeeRepo;
    private readonly IEmailSender _emailSender;
    private readonly IWhatsAppSender _whatsAppSender;
    private readonly IConfiguration _config;
    private readonly IValidator<CreatePublicAppointmentRequest> _validator;
    private readonly ILogger<PublicBookingController> _logger;
    private readonly IEmployeeServicePriceRepository _employeePriceRepo;

    public PublicBookingController(
        IBusinessRepository businessRepo,
        IServiceRepository serviceRepo,
        IAppointmentRepository appointmentRepo,
        INotificationRepository notificationRepo,
        IAvailabilityService availabilityService,
        IEmployeeRepository employeeRepo,
        IEmailSender emailSender,
        IWhatsAppSender whatsAppSender,
        IConfiguration config,
        IValidator<CreatePublicAppointmentRequest> validator,
        ILogger<PublicBookingController> logger,
        IEmployeeServicePriceRepository employeePriceRepo)
    {
        _businessRepo = businessRepo;
        _serviceRepo = serviceRepo;
        _appointmentRepo = appointmentRepo;
        _notificationRepo = notificationRepo;
        _availabilityService = availabilityService;
        _employeeRepo = employeeRepo;
        _emailSender = emailSender;
        _whatsAppSender = whatsAppSender;
        _config = config;
        _validator = validator;
        _logger = logger;
        _employeePriceRepo = employeePriceRepo;
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
        return Ok(new PublicBusinessDto(business.Id, business.Name, business.PublicSlug, business.LogoUrl));
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

    /// <summary>Lista funcionários ativos do negócio (público, para exibir opções de escolha no agendamento).</summary>
    [HttpGet("{slug}/employees")]
    [ProducesResponseType(typeof(IReadOnlyList<EmployeeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEmployees(string slug, CancellationToken cancellationToken = default)
    {
        var business = await _businessRepo.GetByPublicSlugAsync(slug, cancellationToken);
        if (business == null) throw CommException.NotFound("Link de agendamento não encontrado.");
        var employees = await _employeeRepo.GetByBusinessIdAsync(business.Id, onlyActive: true, cancellationToken);
        var prices = await _employeePriceRepo.GetByEmployeeIdsAsync(employees.Select(e => e.Id), cancellationToken);
        var lookup = prices.ToLookup(p => p.EmployeeId, p => new EmployeeServicePriceDto(p.ServiceId, p.Price));
        return Ok(employees.Select(e => new EmployeeResponse(e.Id, e.Name, e.Role, e.IsActive, lookup[e.Id].ToList())));
    }

    /// <summary>Horários do dia com indicação disponível/ocupado. date = yyyy-MM-dd (dia no fuso Brasil). employeeId filtra por funcionário específico.</summary>
    [HttpGet("{slug}/slots")]
    [ProducesResponseType(typeof(IReadOnlyList<SlotWithAvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSlots(string slug, [FromQuery] Guid serviceId, [FromQuery] DateTime date, [FromQuery] Guid? employeeId = null, CancellationToken cancellationToken = default)
    {
        var business = await _businessRepo.GetByPublicSlugAsync(slug, cancellationToken);
        if (business == null) throw CommException.NotFound("Link de agendamento não encontrado.");
        var dateOnly = date.Date;
        var slots = await _availabilityService.GetSlotsWithAvailabilityAsync(business.Id, serviceId, dateOnly, 30, employeeId, cancellationToken);
        return Ok(slots);
    }

    /// <summary>Cria agendamento via link público (sem login).</summary>
    [HttpPost("{slug}/appointments")]
    [Microsoft.AspNetCore.RateLimiting.EnableRateLimiting("public-booking")]
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

        // Valida funcionário, se informado
        Employee? employee = null;
        if (request.EmployeeId.HasValue && request.EmployeeId.Value != Guid.Empty)
        {
            employee = await _employeeRepo.GetByIdAndBusinessIdAsync(request.EmployeeId.Value, business.Id, cancellationToken);
            if (employee == null || !employee.IsActive)
                throw CommException.BadRequest("Funcionário não encontrado ou inativo.");
        }

        // Sempre gravar em UTC. Se o cliente enviar sem "Z" (Unspecified), tratar como horário de Brasília e converter para UTC (evita 14:30 Brasil ser salvo como 14:30 UTC).
        var scheduledAt = ToUtcFromRequest(request.ScheduledAt);

        int? capacity = null;
        if (employee == null)
        {
            var activeCount = await _employeeRepo.CountActiveByBusinessIdAsync(business.Id, cancellationToken);
            if (activeCount > 0) capacity = activeCount;
        }

        var hasConflict = await _appointmentRepo.HasConflictAsync(business.Id, scheduledAt, service.DurationMinutes, null, employee?.Id, capacity, cancellationToken);
        if (hasConflict) throw CommException.Conflict("Este horário não está mais disponível. Escolha outro.");

        var appointment = Appointment.Create(
            business.Id, request.ServiceId, request.ClientName, scheduledAt,
            request.ClientPhone, request.ClientEmail, request.Notes,
            employee?.Id, employee?.Name);
        appointment.SetStatus(AppointmentStatus.Confirmed); // Cliente final agendou = confirmado em Agendamentos
        appointment.SetCancelToken(Guid.NewGuid().ToString("N"));
        await _appointmentRepo.AddAsync(appointment, cancellationToken);

        var notification = Notification.CreateNewAppointment(business.UserId, request.ClientName, scheduledAt, appointment.Id);
        await _notificationRepo.AddAsync(notification, cancellationToken);

        // Usa BaseBookingUrl como primário; Email:PasswordResetBaseUrl como fallback (legado)
        var baseUrl = (_config["BaseBookingUrl"]?.Trim()
                    ?? _config["Email:PasswordResetBaseUrl"]?.Trim()
                    ?? "").TrimEnd('/');
        var cancelLink = string.IsNullOrEmpty(baseUrl)
            ? ""
            : $"{baseUrl}/agendar/cancelar?token={Uri.EscapeDataString(appointment.CancelToken!)}";
        var scheduledFormatted = BrazilTimeHelper.FormatUtcToBrazilDateTime(scheduledAt);

        if (!string.IsNullOrEmpty(request.ClientEmail))
            await _emailSender.SendAppointmentConfirmationAsync(
                request.ClientEmail, request.ClientName, scheduledFormatted, service.Name, business.Name, cancelLink, cancellationToken);

        if (!string.IsNullOrEmpty(request.ClientPhone))
        {
            var whatsMsg = cancelLink != null
                ? $"Olá, {request.ClientName}! Seu agendamento em {business.Name} foi confirmado para {scheduledFormatted}. Serviço: {service.Name}. Para cancelar: {cancelLink}"
                : $"Olá, {request.ClientName}! Seu agendamento em {business.Name} foi confirmado para {scheduledFormatted}. Serviço: {service.Name}.";
            await _whatsAppSender.SendTextAsync(request.ClientPhone, whatsMsg, cancellationToken);
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

        var cancelledFormatted = BrazilTimeHelper.FormatUtcToBrazilDateTime(appointment.ScheduledAt);

        if (!string.IsNullOrEmpty(appointment.ClientEmail))
            await _emailSender.SendAppointmentCancelledByClientAsync(
                appointment.ClientEmail, appointment.ClientName, cancelledFormatted, business.Name, cancellationToken);

        if (!string.IsNullOrEmpty(appointment.ClientPhone))
            await _whatsAppSender.SendTextAsync(appointment.ClientPhone,
                $"Olá, {appointment.ClientName}! Seu agendamento em {business.Name} para {cancelledFormatted} foi cancelado com sucesso.", cancellationToken);

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
