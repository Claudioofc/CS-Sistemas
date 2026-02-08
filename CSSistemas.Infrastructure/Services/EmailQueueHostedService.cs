using System.Threading.Channels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Processa fila de e-mail em background (uma instância por app).</summary>
public sealed class EmailQueueHostedService : BackgroundService
{
    private readonly ChannelReader<EmailWorkItem> _channel;
    private readonly IServiceProvider _services;
    private readonly ILogger<EmailQueueHostedService> _logger;

    public EmailQueueHostedService(Channel<EmailWorkItem> channel, IServiceProvider services, ILogger<EmailQueueHostedService> logger)
    {
        _channel = channel.Reader;
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _channel.ReadAllAsync(stoppingToken))
        {
            try
            {
                using var scope = _services.CreateScope();
                var sender = scope.ServiceProvider.GetKeyedService<CSSistemas.Application.Interfaces.IEmailSender>("real");
                if (sender == null)
                {
                    _logger.LogError("IEmailSender (real) não registrado.");
                    continue;
                }

                switch (item.Kind)
                {
                    case EmailWorkItemKind.PasswordReset:
                        await sender.SendPasswordResetAsync(item.Email!, item.ResetLink!, stoppingToken);
                        break;
                    case EmailWorkItemKind.AppointmentConfirmation:
                        await sender.SendAppointmentConfirmationAsync(item.ToEmail!, item.ClientName!, item.ScheduledAtFormatted!, item.ServiceName!, item.BusinessName!, item.CancelLink!, stoppingToken);
                        break;
                    case EmailWorkItemKind.AppointmentCancelledByProfessional:
                        await sender.SendAppointmentCancelledByProfessionalAsync(item.ToEmail!, item.ClientName!, item.ScheduledAtFormatted!, item.BusinessName!, item.CancellationReason, stoppingToken);
                        break;
                    case EmailWorkItemKind.NewUserRegistered:
                        await sender.SendNewUserRegisteredAsync(item.ToEmail!, item.NewUserRegisteredName!, item.NewUserRegisteredEmail!, stoppingToken);
                        break;
                    case EmailWorkItemKind.SupportRequest:
                        await sender.SendSupportRequestAsync(item.ToEmail!, item.SupportRequestUserName!, item.SupportRequestUserEmail!, item.SupportRequestMessage!, item.SupportRequestPageUrl, stoppingToken);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar e-mail da fila (Kind={Kind})", item.Kind);
            }
        }
    }
}
