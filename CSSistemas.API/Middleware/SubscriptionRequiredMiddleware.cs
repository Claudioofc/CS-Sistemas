using System.Security.Claims;
using CSSistemas.Application.Interfaces;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace CSSistemas.API.Middleware;

/// <summary>
/// Garante que usuários autenticados tenham assinatura ativa (trial ou paga).
/// Rotas em whitelist não são verificadas. Se não houver assinatura ativa, retorna 403.
/// Usuários antigos (sem nenhuma assinatura) recebem trial de 7 dias automaticamente (backfill).
/// </summary>
public class SubscriptionRequiredMiddleware
{
    private static readonly (string Path, string Method)[] Whitelist =
    {
        ("/api/auth/me", "GET"),
        ("/api/auth/profile", "PATCH"),
        ("/api/auth/profile-photo", "POST"),
        ("/api/subscription/status", "GET"),
        ("/api/plans", "GET")
    };

    private readonly RequestDelegate _next;

    public SubscriptionRequiredMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!context.User.Identity?.IsAuthenticated ?? true)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value?.TrimEnd('/') ?? "";
        var method = context.Request.Method;
        if (Whitelist.Any(w => path.Equals(w.Path, StringComparison.OrdinalIgnoreCase) && method.Equals(w.Method, StringComparison.OrdinalIgnoreCase)))
        {
            await _next(context);
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            await _next(context);
            return;
        }

        using var scope = context.RequestServices.CreateScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();

        var subscription = await subscriptionRepo.GetActiveByUserIdAsync(userId, context.RequestAborted);

        if (subscription == null)
        {
            var hasAny = await subscriptionRepo.ExistsAnyByUserIdAsync(userId, context.RequestAborted);
            if (!hasAny)
            {
                var trial = Subscription.CreateTrial(userId);
                await subscriptionRepo.AddAsync(trial, context.RequestAborted);
                subscription = trial;
            }
        }

        if (subscription == null)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                message = "Seu período de teste acabou. Assine um plano para continuar.",
                code = "SubscriptionExpired"
            });
            return;
        }

        await _next(context);
    }
}
