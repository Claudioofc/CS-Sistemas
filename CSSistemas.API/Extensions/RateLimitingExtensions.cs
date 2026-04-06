using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace CSSistemas.API.Extensions;

/// <summary>Rate limiting por IP para alto volume (evita abuso e DoS).</summary>
public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = 429;

            // Limiter estrito para login/registro (5 tentativas/min por IP)
            options.AddFixedWindowLimiter("auth", o =>
            {
                o.PermitLimit = 5;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            // Limiter muito restritivo para forgot-password (2 por hora por IP)
            // Previne: enumeração de e-mails, spam de redefinição, abuso de fila de e-mail
            options.AddFixedWindowLimiter("forgot-password", o =>
            {
                o.PermitLimit = 2;
                o.Window = TimeSpan.FromHours(1);
                o.QueueLimit = 0;
            });

            // Limiter para agendamento público (10 por IP por minuto — evita spam de agendamentos falsos)
            options.AddFixedWindowLimiter("public-booking", o =>
            {
                o.PermitLimit = 10;
                o.Window = TimeSpan.FromMinutes(1);
                o.QueueLimit = 0;
            });

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            options.OnRejected = async (context, _) =>
            {
                context.HttpContext.Response.ContentType = "application/json";
                var retryAfter = 60;
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retry))
                    retryAfter = (int)retry.TotalSeconds;
                context.HttpContext.Response.Headers.RetryAfter = retryAfter.ToString(CultureInfo.InvariantCulture);
                await context.HttpContext.Response.WriteAsJsonAsync(new
                {
                    message = "Muitas requisições. Tente novamente em instantes.",
                    retryAfter
                });
            };
        });

        return services;
    }
}
