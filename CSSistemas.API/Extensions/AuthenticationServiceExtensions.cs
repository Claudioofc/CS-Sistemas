using System.Text;
using CSSistemas.Application.Configuration;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace CSSistemas.API.Extensions;

/// <summary>Configuração de autenticação JWT.</summary>
public static class AuthenticationServiceExtensions
{
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
            ?? throw new InvalidOperationException("JWT não configurado em appsettings.");

        if (string.IsNullOrWhiteSpace(jwtSettings.Secret)
            || jwtSettings.Secret.Length < 32
            || jwtSettings.Secret.Contains("DEFINA")
            || jwtSettings.Secret.Contains("SECRET"))
            throw new InvalidOperationException(
                "Jwt:Secret inválido. Configure um segredo aleatório com no mínimo 32 caracteres em appsettings (nunca use o placeholder padrão).");

        var key = Encoding.UTF8.GetBytes(jwtSettings.Secret);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
                // Lê o JWT do cookie HttpOnly quando não há header Authorization (fallback seguro)
                options.Events = new Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        if (ctx.Token == null && ctx.Request.Cookies.TryGetValue("cssistemas_auth", out var cookieToken))
                            ctx.Token = cookieToken;
                        return Task.CompletedTask;
                    }
                };
            });
        return services;
    }
}
