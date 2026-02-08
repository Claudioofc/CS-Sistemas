using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.Appointment;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Helpers;
using CSSistemas.Application.Interfaces;
using CSSistemas.Application.Validators;
using CSSistemas.API.Extensions;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentRepository _repository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IServiceRepository _serviceRepository;
    private readonly IEmailSender _emailSender;
    private readonly IValidator<AppointmentRequest> _validator;
    private readonly IValidator<AppointmentStatusRequest> _statusValidator;

    public AppointmentsController(
        IAppointmentRepository repository,
        IBusinessRepository businessRepository,
        IServiceRepository serviceRepository,
        IEmailSender emailSender,
        IValidator<AppointmentRequest> validator,
        IValidator<AppointmentStatusRequest> statusValidator)
    {
        _repository = repository;
        _businessRepository = businessRepository;
        _serviceRepository = serviceRepository;
        _emailSender = emailSender;
        _validator = validator;
        _statusValidator = statusValidator;
    }

    /// <summary>Lista agendamentos de um negócio com filtro por nome/telefone e paginação.</summary>
    [HttpGet]
    [Route("by-business/{businessId:guid}")]
    [ProducesResponseType(typeof(PagedAppointmentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByBusiness(
        Guid businessId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var (items, totalCount) = await _repository.GetByBusinessIdPagedAsync(businessId, from, to, search, page, pageSize, cancellationToken);
        var list = items.Select(ToResponse).ToList();
        return Ok(new PagedAppointmentsResponse(list, totalCount));
    }

    /// <summary>Obtém agendamento por id (apenas se pertencer a um negócio do usuário).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var appointment = await _repository.GetByIdAndBusinessIdAsync(id, businessId, cancellationToken);
        if (appointment == null) throw CommException.NotFound("Agendamento não encontrado.");
        return Ok(ToResponse(appointment));
    }

    /// <summary>Cria agendamento (apenas se negócio e serviço pertencerem ao usuário; valida conflito de agenda).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] AppointmentRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _businessRepository.GetByIdAndUserIdAsync(request.BusinessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var service = await _serviceRepository.GetByIdAndBusinessIdAsync(request.ServiceId, request.BusinessId, cancellationToken);
        if (service == null) throw CommException.NotFound("Serviço não encontrado ou não pertence ao negócio.");
        var scheduledAt = request.ScheduledAt.Kind == DateTimeKind.Utc ? request.ScheduledAt : DateTime.SpecifyKind(request.ScheduledAt, DateTimeKind.Utc);
        var hasConflict = await _repository.HasConflictAsync(request.BusinessId, scheduledAt, service.DurationMinutes, null, cancellationToken);
        if (hasConflict) throw CommException.Conflict("Já existe um agendamento neste horário.");
        var appointment = Appointment.Create(
            request.BusinessId, request.ServiceId, request.ClientName, scheduledAt,
            request.ClientPhone, request.ClientEmail, request.Notes);
        await _repository.AddAsync(appointment, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = appointment.Id, businessId = appointment.BusinessId }, ToResponse(appointment));
    }

    /// <summary>Atualiza status do agendamento (apenas se pertencer a um negócio do usuário). Ao marcar Cancelado, envia e-mail ao cliente com justificativa opcional.</summary>
    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(AppointmentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromQuery] Guid businessId, [FromBody] AppointmentStatusRequest statusRequest, CancellationToken cancellationToken = default)
    {
        var validation = await _statusValidator.ValidateAsync(statusRequest, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var appointment = await _repository.GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (appointment == null) throw CommException.NotFound("Agendamento não encontrado.");
        if (statusRequest.Status == AppointmentStatus.Cancelled)
            await SendCancellationEmailIfNeededAsync(appointment, business.Name, statusRequest.CancellationReason, cancellationToken);
        appointment.SetStatus(statusRequest.Status);
        await _repository.UpdateAsync(appointment, cancellationToken);
        return Ok(ToResponse(appointment));
    }

    /// <summary>Soft delete: marca agendamento como excluído (não remove do banco). Não envia e-mail ao cliente — apenas remove da lista.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var deleted = await _repository.SoftDeleteAsync(id, businessId, cancellationToken);
        if (!deleted) throw CommException.NotFound("Agendamento não encontrado.");
        return NoContent();
    }

    private async Task SendCancellationEmailIfNeededAsync(Appointment appointment, string businessName, string? cancellationReason, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(appointment.ClientEmail)) return;
        var scheduledFormatted = BrazilTimeHelper.FormatUtcToBrazilDateTime(appointment.ScheduledAt);
        await _emailSender.SendAppointmentCancelledByProfessionalAsync(
            appointment.ClientEmail, appointment.ClientName, scheduledFormatted, businessName, cancellationReason, cancellationToken);
    }

    private static AppointmentResponse ToResponse(Appointment a) => new(
        a.Id, a.BusinessId, a.ServiceId, a.ClientName, a.ClientPhone, a.ClientEmail,
        a.ScheduledAt, a.Status, a.Notes, a.CreatedAt, a.UpdatedAt);

}

public record PagedAppointmentsResponse(IReadOnlyList<AppointmentResponse> Items, int TotalCount);
