using CSSistemas.API.Extensions;
using CSSistemas.Application.DTOs.Subscription;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController : ControllerBase
{
    private readonly ISubscriptionRepository _subscriptionRepository;

    public SubscriptionController(ISubscriptionRepository subscriptionRepository)
    {
        _subscriptionRepository = subscriptionRepository;
    }

    /// <summary>Status da assinatura do usuário (trial ou paga). Usado para exibir "X dias restantes" ou redirecionar quando expirado.</summary>
    [HttpGet("status")]
    [ProducesResponseType(typeof(SubscriptionStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatus(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId.Value, cancellationToken);
        if (subscription == null)
        {
            return Ok(new SubscriptionStatusResponse(
                HasAccess: false,
                EndsAt: null,
                IsTrial: false,
                DaysRemaining: null
            ));
        }

        var now = DateTime.UtcNow;
        var daysRemaining = (int)Math.Ceiling((subscription.EndsAt - now).TotalDays);
        if (daysRemaining < 0) daysRemaining = 0;

        return Ok(new SubscriptionStatusResponse(
            HasAccess: true,
            EndsAt: subscription.EndsAt,
            IsTrial: subscription.SubscriptionType == SubscriptionType.Trial,
            DaysRemaining: daysRemaining
        ));
    }
}
