using CSSistemas.API.Authorization;
using CSSistemas.API.Mappers;
using CSSistemas.Application.DTOs.Client;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

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
    private readonly IPlanRepository _planRepository;
    private readonly IEmailSender _realEmailSender;
    private readonly ILogger<AdminController> _logger;

    public AdminController(
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        IBusinessRepository businessRepository,
        IClientRepository clientRepository,
        IPlanRepository planRepository,
        [FromKeyedServices("real")] IEmailSender realEmailSender,
        ILogger<AdminController> logger)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _businessRepository = businessRepository;
        _clientRepository = clientRepository;
        _planRepository = planRepository;
        _realEmailSender = realEmailSender;
        _logger = logger;
    }

    /// <summary>Envia e-mail de teste direto (sem fila) para diagnóstico de SMTP. Apenas admin.</summary>
    [HttpPost("test-email")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> TestEmail([FromQuery] string to, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(to))
            return BadRequest(new { error = "Informe o e-mail de destino via ?to=email" });
        try
        {
            await _realEmailSender.SendPasswordResetAsync(to.Trim(), "https://teste-smtp-cs-sistemas", cancellationToken);
            return Ok(new { message = $"E-mail enviado com sucesso para {to.Trim()}" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de teste para {Email}", to.Trim());
            return StatusCode(500, new { error = "Falha ao enviar e-mail. Verifique os logs do servidor." });
        }
    }

    /// <summary>Lista clientes do sistema com busca opcional por nome ou e-mail. Apenas admin.</summary>
    [HttpGet("users")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminUserResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListUsers([FromQuery] string? search, CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetAllAsync(search, cancellationToken);

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

    /// <summary>Registro de assinaturas premium (quem assinou, quando e valor). Apenas admin.</summary>
    [HttpGet("subscriptions/premium")]
    [ProducesResponseType(typeof(IReadOnlyList<AdminPremiumSubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListPremiumSubscriptions([FromQuery] int limit = 100, CancellationToken cancellationToken = default)
    {
        var list = await _subscriptionRepository.GetPremiumSubscriptionsOrderedByStartedAtAsync(Math.Clamp(limit, 1, 500), cancellationToken);
        var plans = await _planRepository.GetActiveAsync(cancellationToken);

        var response = list.Select(s =>
        {
            var billingMonths = (int)Math.Round((s.EndsAt - s.StartedAt).TotalDays / 30.0);
            var plan = plans.OrderBy(p => Math.Abs(p.BillingIntervalMonths - billingMonths)).FirstOrDefault();
            return new AdminPremiumSubscriptionResponse(
                s.UserId,
                s.User?.Name ?? "",
                s.User?.Email ?? "",
                s.StartedAt,
                s.EndsAt,
                plan?.Name ?? "Premium",
                plan?.Price ?? 0);
        }).ToList();

        return Ok(response);
    }
}

/// <summary>Resposta de cliente para painel admin (todos os cadastros, premium e gratuito).</summary>
public record AdminUserResponse(Guid Id, string Email, string Name, DateTime CreatedAt, bool IsAdmin, string SubscriptionLabel);

/// <summary>Resposta de negócio para painel admin (lista todas as clínicas).</summary>
public record AdminBusinessResponse(Guid Id, Guid UserId, string OwnerName, string Name, BusinessType BusinessType, string? PublicSlug, DateTime CreatedAt, DateTime? UpdatedAt);

/// <summary>Registro de assinatura premium para painel admin (quem assinou, quando e valor do plano).</summary>
public record AdminPremiumSubscriptionResponse(Guid UserId, string UserName, string UserEmail, DateTime StartedAt, DateTime EndsAt, string PlanName, decimal Price);
