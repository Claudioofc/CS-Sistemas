using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.SystemMessage;
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
public class SystemMessagesController : ControllerBase
{
    private readonly ISystemMessageRepository _repository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IValidator<SystemMessageRequest> _validator;

    public SystemMessagesController(
        ISystemMessageRepository repository,
        IBusinessRepository businessRepository,
        IValidator<SystemMessageRequest> validator)
    {
        _repository = repository;
        _businessRepository = businessRepository;
        _validator = validator;
    }

    /// <summary>Lista mensagens do sistema (templates) de um negócio.</summary>
    [HttpGet]
    [Route("by-business/{businessId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<SystemMessageResponse>), StatusCodes.Status200OK)]
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

    /// <summary>Obtém mensagem por id (apenas se pertencer a um negócio do usuário).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SystemMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var message = await _repository.GetByIdAndBusinessIdAsync(id, businessId, cancellationToken);
        if (message == null) throw CommException.NotFound("Mensagem não encontrada.");
        return Ok(ToResponse(message));
    }

    /// <summary>Cria template de mensagem em um negócio.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(SystemMessageResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] SystemMessageRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _businessRepository.GetByIdAndUserIdAsync(request.BusinessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var key = request.Key.Trim().ToLowerInvariant();
        var existing = await _repository.GetByBusinessIdAndKeyAsync(request.BusinessId, key, cancellationToken);
        if (existing != null) throw CommException.BadRequest("Já existe uma mensagem com esta chave neste negócio.");
        var message = SystemMessage.Create(request.BusinessId, request.Key, request.Title, request.Body ?? string.Empty);
        await _repository.AddAsync(message, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = message.Id, businessId = message.BusinessId }, ToResponse(message));
    }

    /// <summary>Atualiza template de mensagem.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SystemMessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] SystemMessageRequest request, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        if (request.BusinessId != businessId) throw CommException.BadRequest("BusinessId do corpo deve ser igual ao da URL.");
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var message = await _repository.GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (message == null) throw CommException.NotFound("Mensagem não encontrada.");
        var key = request.Key.Trim().ToLowerInvariant();
        var existingByKey = await _repository.GetByBusinessIdAndKeyAsync(businessId, key, cancellationToken);
        if (existingByKey != null && existingByKey.Id != id) throw CommException.BadRequest("Já existe outra mensagem com esta chave neste negócio.");
        message.Update(request.Key, request.Title, request.Body ?? string.Empty);
        await _repository.UpdateAsync(message, cancellationToken);
        return Ok(ToResponse(message));
    }

    /// <summary>Soft delete: marca mensagem como excluída.</summary>
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
        if (!deleted) throw CommException.NotFound("Mensagem não encontrada.");
        return NoContent();
    }

    private static SystemMessageResponse ToResponse(SystemMessage m) => new(
        m.Id, m.BusinessId, m.Key, m.Title, m.Body, m.IsActive, m.CreatedAt, m.UpdatedAt);
}
