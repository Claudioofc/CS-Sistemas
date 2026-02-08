using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Enfileira envio de e-mail para processamento em background (não bloqueia o request).</summary>
public sealed class QueuedEmailSender : IEmailSender
{
    private readonly ChannelWriter<EmailWorkItem> _channel;
    private readonly ILogger<QueuedEmailSender> _logger;

    public QueuedEmailSender(Channel<EmailWorkItem> channel, ILogger<QueuedEmailSender> logger)
    {
        _channel = channel.Writer;
        _logger = logger;
    }

    public Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new EmailWorkItem(EmailWorkItemKind.PasswordReset, email, resetLink, null, null, null, null, null, null, null), cancellationToken);
    }

    public Task SendAppointmentConfirmationAsync(string toEmail, string clientName, string scheduledAtFormatted, string serviceName, string businessName, string cancelLink, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new EmailWorkItem(EmailWorkItemKind.AppointmentConfirmation, null, null, toEmail, clientName, scheduledAtFormatted, serviceName, businessName, cancelLink, null), cancellationToken);
    }

    public Task SendAppointmentCancelledByProfessionalAsync(string toEmail, string clientName, string scheduledAtFormatted, string businessName, string? cancellationReason, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new EmailWorkItem(EmailWorkItemKind.AppointmentCancelledByProfessional, null, null, toEmail, clientName, scheduledAtFormatted, null, businessName, null, cancellationReason, null, null), cancellationToken);
    }

    public Task SendNewUserRegisteredAsync(string toEmail, string newUserName, string newUserEmail, CancellationToken cancellationToken = default)
    {
        return EnqueueAsync(new EmailWorkItem(EmailWorkItemKind.NewUserRegistered, null, null, toEmail, null, null, null, null, null, null, newUserName, newUserEmail), cancellationToken);
    }

    private async Task EnqueueAsync(EmailWorkItem item, CancellationToken cancellationToken)
    {
        if (await _channel.WaitToWriteAsync(cancellationToken))
        {
            await _channel.WriteAsync(item, cancellationToken);
            return;
        }
        _logger.LogWarning("Fila de e-mail cheia; mensagem não enfileirada (Kind={Kind})", item.Kind);
    }
}
