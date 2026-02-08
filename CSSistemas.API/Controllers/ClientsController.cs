using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.Client;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using CSSistemas.API.Extensions;
using CSSistemas.API.Mappers;
using CSSistemas.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _repository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IValidator<ClientRequest> _validator;

    public ClientsController(
        IClientRepository repository,
        IBusinessRepository businessRepository,
        IValidator<ClientRequest> validator)
    {
        _repository = repository;
        _businessRepository = businessRepository;
        _validator = validator;
    }

    /// <summary>Lista clientes de um negócio (apenas se o negócio pertencer ao usuário).</summary>
    [HttpGet]
    [Route("by-business/{businessId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListByBusiness(Guid businessId, [FromQuery] bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var list = await _repository.GetByBusinessIdAsync(businessId, onlyActive: !includeInactive, cancellationToken);
        return Ok(list.Select(ClientResponseMapper.ToResponse));
    }

    /// <summary>Obtém cliente por id (apenas se pertencer a um negócio do usuário).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var client = await _repository.GetByIdAndBusinessIdAsync(id, businessId, cancellationToken);
        if (client == null) throw CommException.NotFound("Cliente não encontrado.");
        return Ok(ClientResponseMapper.ToResponse(client));
    }

    /// <summary>Cria cliente em um negócio (apenas se o negócio pertencer ao usuário).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create([FromBody] ClientRequest request, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _businessRepository.GetByIdAndUserIdAsync(request.BusinessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var client = Client.Create(request.BusinessId, request.Name, request.Phone, request.Email, request.Notes);
        await _repository.AddAsync(client, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = client.Id, businessId = client.BusinessId }, ClientResponseMapper.ToResponse(client));
    }

    /// <summary>Atualiza cliente (apenas se pertencer a um negócio do usuário).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClientResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] ClientRequest request, [FromQuery] Guid businessId, CancellationToken cancellationToken = default)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        if (request.BusinessId != businessId) throw CommException.BadRequest("BusinessId do corpo deve ser igual ao da URL.");
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _businessRepository.GetByIdAndUserIdAsync(businessId, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var client = await _repository.GetByIdAndBusinessIdForUpdateAsync(id, businessId, cancellationToken);
        if (client == null) throw CommException.NotFound("Cliente não encontrado.");
        client.Update(request.Name, request.Phone, request.Email, request.Notes);
        await _repository.UpdateAsync(client, cancellationToken);
        return Ok(ClientResponseMapper.ToResponse(client));
    }

    /// <summary>Soft delete: marca cliente como excluído (não remove do banco).</summary>
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
        if (!deleted) throw CommException.NotFound("Cliente não encontrado.");
        return NoContent();
    }
}
