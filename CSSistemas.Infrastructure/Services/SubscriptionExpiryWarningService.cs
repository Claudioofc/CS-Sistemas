using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Serviço em background que roda diariamente e envia e-mail de aviso quando a assinatura vence em 7 ou 1 dia.</summary>
public sealed class SubscriptionExpiryWarningService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SubscriptionExpiryWarningService> _logger;

    public SubscriptionExpiryWarningService(IServiceProvider services, ILogger<SubscriptionExpiryWarningService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Aguarda 30 segundos na inicialização para o app subir completamente
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessWarningsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar avisos de vencimento de assinatura.");
            }

            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task ProcessWarningsAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var subscriptionRepo = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var planRepo = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
        var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

        var plans = await planRepo.GetActiveAsync(cancellationToken);

        foreach (var daysBeforeExpiry in new[] { 7, 1 })
        {
            var expiring = await subscriptionRepo.GetExpiringForWarningAsync(daysBeforeExpiry, cancellationToken);

            foreach (var sub in expiring)
            {
                try
                {
                    var userEmail = sub.User?.Email;
                    var userName = sub.User?.Name ?? "Cliente";
                    if (string.IsNullOrWhiteSpace(userEmail)) continue;

                    // Determina o plano pelo intervalo de cobrança
                    var billingMonths = (int)Math.Round((sub.EndsAt - sub.StartedAt).TotalDays / 30.0);
                    var plan = plans.OrderBy(p => Math.Abs(p.BillingIntervalMonths - billingMonths)).FirstOrDefault();
                    var planName = plan?.Name ?? "Premium";

                    var endsAtFormatted = sub.EndsAt.ToLocalTime().ToString("dd/MM/yyyy");

                    await emailSender.SendSubscriptionExpiryWarningAsync(userEmail, userName, planName, endsAtFormatted, daysBeforeExpiry, cancellationToken);

                    if (daysBeforeExpiry == 7)
                        sub.MarkExpiryWarning7DaySent();
                    else
                        sub.MarkExpiryWarning1DaySent();

                    await subscriptionRepo.UpdateAsync(sub, cancellationToken);
                    _logger.LogInformation("Aviso de vencimento ({Days} dias) enviado para {Email}", daysBeforeExpiry, userEmail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro ao enviar aviso de vencimento para assinatura {Id}", sub.Id);
                }
            }
        }
    }
}
