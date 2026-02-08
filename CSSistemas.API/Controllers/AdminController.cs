using CSSistemas.API.Authorization;
using CSSistemas.API.Mappers;
using CSSistemas.Application.DTOs.Client;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Policy = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IBusinessRepository _businessRepository;
    private readonly IClientRepository _clientRepository;

    public AdminController(
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IBusinessRepository businessRepository,
        IClientRepository clientRepository)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _businessRepository = businessRepository;
        _clientRepository = clientRepository;
    }

    /// <summary>Lista todos os clientes do sistema (premium e gratuito). Apenas admin.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(cancellationToken);
        var userIds = users.Select(u => u.Id).ToList();
        var activeSubs = await _subscriptionRepository.GetActiveByUserIdsAsync(userIds, cancellationToken);
        var subByUser = activeSubs.ToDictionary(s => s.UserId, s => s.SubscriptionType);

        var response = users.Select(u =>
        {
            var subType = subByUser.TryGetValue(u.Id, out var st) ? st : (SubscriptionType?)null;
            var subscriptionLabel = subType switch
            {
                SubscriptionType.Trial => "Gratuito",
                SubscriptionType.Monthly => "Premium",
                _ => "Gratuito"
            };
            return new AdminUserResponse(
                u.Id,
                u.Email,
                u.Name,
                u.CreatedAt,
                u.IsAdmin,
                subscriptionLabel
            );
        }).ToList();
        return Ok(response);
    }

    /// <summary>Lista todos os negócios do sistema (para admin ver clientes de qualquer clínica).</summary>
    [HttpGet("businesses")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminBusinessResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListBusinesses(CancellationToken cancellationToken)
    {
        var list = await _businessRepository.GetAllAsync(cancellationToken);
        var response = list.Select(b => new AdminBusinessResponse(
            b.Id,
            b.UserId,
            b.User?.Name ?? "",
            b.Name,
            b.BusinessType,
            b.PublicSlug,
            b.WhatsAppPhone,
            b.CreatedAt,
            b.UpdatedAt)).ToList();
        return Ok(response);
    }

    /// <summary>Lista clientes ativos de um negócio (qualquer negócio, apenas admin).</summary>
    [HttpGet("clients/by-business/{businessId:guid}")]
    [ProducesResponseType(typeof(IReadOnlyList<ClientResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListClientsByBusiness(Guid businessId, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(businessId, cancellationToken);
        if (business == null) return NotFound();
        var list = await _clientRepository.GetByBusinessIdAsync(businessId, onlyActive: true, cancellationToken);
        return Ok(list.Select(ClientResponseMapper.ToResponse));
    }

    /// <summary>Registro de assinaturas premium (quem assinou e quando). Apenas admin.</summary>
    [HttpGet("subscriptions/premium")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminPremiumSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPremiumSubscriptions([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _subscriptionRepository.GetPremiumSubscriptionsOrderedByStartedAtAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        var response = list.Select(s => new AdminPremiumSubscriptionResponse(
            s.UserId,
            s.User?.Name ?? "",
            s.User?.Email ?? "",
            s.StartedAt,
            s.EndsAt)).ToList();
        return Ok(response);
    }
}

/// <summary>Resposta de cliente para painel admin (todos os cadastros, premium e gratuito).</summary>
public record AdminUserResponse(Guid Id, string Email, string Name, DateTime CreatedAt, bool IsAdmin, string SubscriptionLabel);

/// <summary>Resposta de negócio para painel admin (lista todas as clínicas).</summary>
public record AdminBusinessResponse(Guid Id, Guid UserId, string OwnerName, string Name, BusinessType BusinessType, string? PublicSlug, string? WhatsAppPhone, DateTime CreatedAt, DateTime? UpdatedAt);

/// <summary>Registro de assinatura premium para painel admin (quem assinou e quando).</summary>
public record AdminPremiumSubscriptionResponse(Guid UserId, string UserName, string UserEmail, DateTime StartedAt, DateTime EndsAt);
