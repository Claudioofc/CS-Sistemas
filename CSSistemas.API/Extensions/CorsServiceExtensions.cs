using Microsoft.Extensions.DependencyInjection;

namespace CSSistemas.API.Extensions;

/// <summary>Configuração de CORS.</summary>
public static class CorsServiceExtensions
{
    public static IServiceCollection AddApiCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });
        return services;
    }
}
