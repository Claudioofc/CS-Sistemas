using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.Service;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using CSSistemas.API.Extensions;
using CSSistemas.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ServicesController : ControllerBase
{
    private readonly IServiceRepository _repository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IValidator<ServiceRequest> _validator;

    public ServicesController(
        IServiceRepository repository,
        IBusinessRepository businessRepository,
        IValidator<ServiceRequest> validator)
    {
        _repository = repository;
        _businessRepository = businessRepository;
        _validator = validator;
    }

    /// <summary>Lista serviços de um negócio (apenas se o negócio pertencer ao usuário).</summary>
    [HttpGet]
    [Route("by-business/{businessId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ServiceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByBusiness(Guid businessId, [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var list = await _repository.GetByBusinessIdAsync(businessId, onlyActive: !includeInactive, cancellationToken);
        return Ok(list.Select(ToResponse));
    }

    /// <summary>Obtém serviço por id (apenas se pertencer a um negócio do usuário).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var service = await _repository.GetByIdAndBusinessIdAsync(id, businessId, cancellationToken);
        if (service == null) throw CommException.NotFound("Serviço não encontrado.");
        return Ok(ToResponse(service));
    }

    /// <summary>Cria serviço em um negócio (apenas se o negócio pertencer ao usuário).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] ServiceRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _businessRepository.GetByIdAndUserIdAsync(request.BusinessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var service = Service.Create(request.BusinessId, request.Name, request.DurationMinutes, request.Price);
        await _repository.AddAsync(service, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = service.Id, businessId = service.BusinessId }, ToResponse(service));
    }

    /// <summary>Atualiza serviço (apenas se pertencer a um negócio do usuário).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ServiceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ServiceRequest request, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        if (request.BusinessId != businessId) throw CommException.BadRequest("BusinessId do corpo deve ser igual ao da URL.");
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var service = await _repository.GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (service == null) throw CommException.NotFound("Serviço não encontrado.");
        service.Update(request.Name, request.DurationMinutes, request.Price);
        await _repository.UpdateAsync(service, cancellationToken);
        return Ok(ToResponse(service));
    }

    /// <summary>Soft delete: marca serviço como excluído (não remove do banco).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid businessId, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var deleted = await _repository.SoftDeleteAsync(id, businessId, cancellationToken);
        if (!deleted) throw CommException.NotFound("Serviço não encontrado.");
        return NoContent();
    }

    private static ServiceResponse ToResponse(Service s) => new(
        s.Id, s.BusinessId, s.Name, s.DurationMinutes, s.Price, s.IsActive, s.CreatedAt, s.UpdatedAt);
}
