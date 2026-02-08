using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using CSSistemas.Infrastructure.Data;
using CSSistemas.Infrastructure.Repositories;
using CSSistemas.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Http;
using System.Threading.Channels;

namespace CSSistemas.Infrastructure;

/// <summary>
/// Registro de serviços da Infrastructure (DRY).
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' não configurada.");

        // Pool para alto volume (Npgsql aceita Maximum Pool Size e Minimum Pool Size na connection string).
        if (!connectionString.Contains("Maximum Pool Size", StringComparison.OrdinalIgnoreCase))
            connectionString += ";Maximum Pool Size=200";
        if (!connectionString.Contains("Minimum Pool Size", StringComparison.OrdinalIgnoreCase))
            connectionString += ";Minimum Pool Size=10";

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));
        services.Configure<EmailSettings>(configuration.GetSection(EmailSettings.SectionName));
        services.Configure<AdminSettings>(configuration.GetSection(AdminSettings.SectionName));
        services.Configure<PaymentSettings>(configuration.GetSection(PaymentSettings.SectionName));
        services.Configure<OpenAISettings>(configuration.GetSection(OpenAISettings.SectionName));
        services.Configure<WhatsAppSettings>(configuration.GetSection(WhatsAppSettings.SectionName));

        services.AddHttpClient<IOpenAIChatService, OpenAIChatService>();

        var redisConfig = configuration["Redis:Configuration"] ?? configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConfig))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConfig);
            services.AddSingleton<IPendingWhatsAppSlotStore, RedisPendingWhatsAppSlotStore>();
        }
        else
        {
            services.AddSingleton<IPendingWhatsAppSlotStore, InMemoryPendingWhatsAppSlotStore>();
        }

        services.AddScoped<IWhatsAppSender, WhatsAppSenderStub>();

        services.AddScoped<EmailSender>();
        services.AddScoped<ResendEmailSender>();
        services.AddKeyedScoped<IEmailSender>("real", (sp, _) =>
        {
            var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailSettings>>().Value;
            if (string.Equals(settings.Provider, "Resend", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(settings.ResendApiKey))
                return sp.GetRequiredService<ResendEmailSender>();
            return sp.GetRequiredService<EmailSender>();
        });
        services.AddSingleton(Channel.CreateUnbounded<EmailWorkItem>(new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }));
        services.AddSingleton<IEmailSender, QueuedEmailSender>();
        services.AddHostedService<EmailQueueHostedService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IBusinessRepository, BusinessRepository>();
        services.AddScoped<IServiceRepository, ServiceRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();
        services.AddScoped<ISystemMessageRepository, SystemMessageRepository>();
        services.AddScoped<IAppointmentRepository, AppointmentRepository>();
        services.AddScoped<INotificationRepository, NotificationRepository>();
        services.AddScoped<IBusinessHoursRepository, BusinessHoursRepository>();
        services.AddScoped<IAvailabilityService, AvailabilityService>();
        services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();
        services.AddScoped<IPlanRepository, PlanRepository>();
        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
