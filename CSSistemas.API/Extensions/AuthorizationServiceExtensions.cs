using CSSistemas.API.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace CSSistemas.API.Extensions;

/// <summary>Configuração de autorização (políticas e handlers).</summary>
public static class AuthorizationServiceExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy("Admin", policy => policy.Requirements.Add(new AdminRequirement()));
        });
        services.AddScoped<IAuthorizationHandler, AdminAuthorizationHandler>();
        return services;
    }
}
