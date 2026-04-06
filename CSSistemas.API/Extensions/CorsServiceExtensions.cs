using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace CSSistemas.API.Extensions;

/// <summary>Configuração de CORS — origens permitidas via BaseBookingUrl + CorsAllowedOrigins.</summary>
public static class CorsServiceExtensions
{
    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var baseUrl = configuration["BaseBookingUrl"]?.TrimEnd('/');
        var extra = configuration.GetSection("CorsAllowedOrigins").Get<string[]>() ?? [];

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(baseUrl)) allowed.Add(baseUrl);
        foreach (var o in extra) if (!string.IsNullOrWhiteSpace(o)) allowed.Add(o.TrimEnd('/'));

        // Localhost permitido apenas em desenvolvimento local
        if (environment.IsDevelopment())
        {
            allowed.Add("http://localhost:5173");
            allowed.Add("http://localhost:5264");
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins([.. allowed])
                      .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                      .WithHeaders("Content-Type", "Authorization")
                      .AllowCredentials();
            });
        });
        return services;
    }
}
