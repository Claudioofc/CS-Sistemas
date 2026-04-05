using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Envio de e-mail via SMTP usando MailKit. Se não configurado, apenas loga (desenvolvimento).</summary>
public class EmailSender : IEmailSender
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailSender> _logger;

    public EmailSender(IOptions<EmailSettings> settings, ILogger<EmailSender> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Link de redefinição para {Email}: {Link}", email, resetLink);
            return;
        }

        var message = BuildMessage(email, "Redefinição de senha - CS Sistemas",
            $"Você solicitou a redefinição de senha.\n\nClique no link abaixo para definir uma nova senha (válido por 1 hora):\n\n{resetLink}\n\nSe você não solicitou isso, ignore este e-mail.\n\n— CS Sistemas");

        await SendAsync(message, cancellationToken);
    }

    public async Task SendAppointmentConfirmationAsync(string toEmail, string clientName, string scheduledAtFormatted, string serviceName, string businessName, string cancelLink, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Confirmação para {Email}: {BusinessName}, {ScheduledAt}. Cancelar: {Link}", toEmail, businessName, scheduledAtFormatted, cancelLink);
            return;
        }

        var cancelBlock = string.IsNullOrWhiteSpace(cancelLink)
            ? "\nPara cancelar, entre em contato com o estabelecimento."
            : $"\nPara cancelar, use o link abaixo:\n{cancelLink}";
        var body = $"Olá, {clientName}.\n\nSeu agendamento em {businessName} foi confirmado.\n\nServiço: {serviceName}\nData/hora: {scheduledAtFormatted}{cancelBlock}\n\n— CS Sistemas";
        var message = BuildMessage(toEmail, "Agendamento confirmado - " + businessName, body);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendAppointmentCancelledByProfessionalAsync(string toEmail, string clientName, string scheduledAtFormatted, string businessName, string? cancellationReason = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Cancelamento para {Email}: {BusinessName}, {ScheduledAt}", toEmail, businessName, scheduledAtFormatted);
            return;
        }

        var reasonBlock = !string.IsNullOrWhiteSpace(cancellationReason)
            ? $"\nMotivo informado: {cancellationReason.Trim()}\n"
            : "";
        var body = $"Olá, {clientName}.\n\nInfelizmente seu agendamento em {businessName} para o dia {scheduledAtFormatted} foi cancelado.{reasonBlock}\nSe quiser reagendar, entre em contato com o estabelecimento.\n\n— CS Sistemas";
        var message = BuildMessage(toEmail, "Agendamento cancelado - " + businessName, body);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendAppointmentCancelledByClientAsync(string toEmail, string clientName, string scheduledAtFormatted, string businessName, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Cancelamento pelo cliente para {Email}: {BusinessName}, {ScheduledAt}", toEmail, businessName, scheduledAtFormatted);
            return;
        }

        var body = $"Olá, {clientName}.\n\nSeu agendamento em {businessName} para o dia {scheduledAtFormatted} foi cancelado com sucesso.\n\nSe quiser reagendar, acesse o link de agendamento do estabelecimento.\n\n— CS Sistemas";
        var message = BuildMessage(toEmail, "Agendamento cancelado - " + businessName, body);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendNewUserRegisteredAsync(string toEmail, string newUserName, string newUserEmail, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Novo usuário: {Name} ({Email})", newUserName, newUserEmail);
            return;
        }

        var body = $"Um novo usuário se cadastrou no sistema.\n\nNome: {newUserName}\nE-mail: {newUserEmail}\n\n— CS Sistemas";
        var message = BuildMessage(toEmail, "Novo cadastro - CS Sistemas", body);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendWelcomeToNewUserAsync(string toEmail, string userName, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Boas-vindas para: {Name} ({Email})", userName, toEmail);
            return;
        }

        var message = BuildMessage(toEmail, WelcomeEmailContent.Subject, WelcomeEmailContent.BuildPlainTextBody(userName));
        await SendAsync(message, cancellationToken);
    }

    public async Task SendSupportRequestAsync(string toEmail, string userName, string userEmail, string message, string? pageUrl = null, byte[]? attachment = null, string? attachmentFileName = null, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Suporte: {Name} ({Email}): {Message}", userName, userEmail, message);
            return;
        }

        var body = SupportRequestEmailContent.BuildPlainTextBody(userName, userEmail, message, pageUrl);
        var mimeMessage = BuildMessage(toEmail, SupportRequestEmailContent.Subject, body);

        if (attachment != null && !string.IsNullOrWhiteSpace(attachmentFileName))
        {
            var builder = new BodyBuilder { TextBody = body };
            builder.Attachments.Add(attachmentFileName, attachment);
            mimeMessage.Body = builder.ToMessageBody();
        }

        await SendAsync(mimeMessage, cancellationToken);
    }

    public async Task SendSubscriptionExpiryWarningAsync(string toEmail, string userName, string planName, string endsAtFormatted, int daysRemaining, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Aviso de vencimento para {Email}: plano {Plan}, vence em {Days} dias", toEmail, planName, daysRemaining);
            return;
        }
        var body = $"Olá, {userName}.\n\nSua assinatura {planName} vence em {daysRemaining} dia(s) ({endsAtFormatted}).\n\nRenove agora mesmo para continuar usando o CS Sistemas sem interrupções.\n\n— CS Sistemas";
        var message = BuildMessage(toEmail, $"Sua assinatura vence em {daysRemaining} dia(s) - CS Sistemas", body);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendAppointmentReminderAsync(string toEmail, string clientName, string scheduledAtFormatted, string serviceName, string businessName, string cancelLink, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Lembrete para {Email}: {BusinessName}, {ScheduledAt}", toEmail, businessName, scheduledAtFormatted);
            return;
        }
        var body = $"Olá, {clientName}.\n\nEste é um lembrete do seu agendamento em {businessName}.\n\nServiço: {serviceName}\nData/hora: {scheduledAtFormatted}\n\nCaso precise cancelar, use o link abaixo:\n{cancelLink}\n\n— CS Sistemas";
        var message = BuildMessage(toEmail, "Lembrete de agendamento - " + businessName, body);
        await SendAsync(message, cancellationToken);
    }

    public async Task SendEmailVerificationAsync(string toEmail, string userName, string code, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured())
        {
            _logger.LogInformation("E-mail não configurado. Código de verificação para {Email}: {Code}", toEmail, code);
            return;
        }
        var body = $"Olá, {userName}.\n\nSeu código de verificação de e-mail é:\n\n{code}\n\nEste código expira em 10 minutos. Não compartilhe com ninguém.\n\nSe não foi você, ignore este e-mail.\n\n— CS Sistemas";
        var message = BuildMessage(toEmail, "Confirme seu e-mail - CS Sistemas", body);
        await SendAsync(message, cancellationToken);
    }

    // -------------------------------------------------------------------------

    private bool IsConfigured()
    {
        return !string.IsNullOrWhiteSpace(_settings.SmtpHost)
            && !string.IsNullOrWhiteSpace(_settings.SmtpUser)
            && !string.IsNullOrWhiteSpace(_settings.SmtpPassword);
    }

    private MimeMessage BuildMessage(string toEmail, string subject, string plainTextBody)
    {
        var fromName = string.IsNullOrWhiteSpace(_settings.FromName) ? "CS Sistemas" : _settings.FromName;

        // Se o FromEmail for de um provedor gratuito (Gmail, Yahoo, Hotmail, etc.),
        // usar o SmtpUser como From (domínio autenticado no Brevo) e o FromEmail no Reply-To.
        // Isso evita falha de DMARC no Yahoo, Outlook e outros provedores rígidos.
        var configuredFrom = _settings.FromEmail?.Trim();
        var isFreeProvider = !string.IsNullOrWhiteSpace(configuredFrom)
            && (configuredFrom.EndsWith("@gmail.com", StringComparison.OrdinalIgnoreCase)
             || configuredFrom.EndsWith("@yahoo.com", StringComparison.OrdinalIgnoreCase)
             || configuredFrom.EndsWith("@yahoo.com.br", StringComparison.OrdinalIgnoreCase)
             || configuredFrom.EndsWith("@hotmail.com", StringComparison.OrdinalIgnoreCase)
             || configuredFrom.EndsWith("@outlook.com", StringComparison.OrdinalIgnoreCase)
             || configuredFrom.EndsWith("@live.com", StringComparison.OrdinalIgnoreCase));

        string from;
        string? replyTo = null;

        if (isFreeProvider)
        {
            // Usa o SmtpUser como From (tem SPF/DKIM válido no Brevo)
            from = _settings.SmtpUser!.Trim();
            replyTo = configuredFrom; // Respostas chegam no e-mail configurado
        }
        else
        {
            from = string.IsNullOrWhiteSpace(configuredFrom) ? _settings.SmtpUser!.Trim() : configuredFrom;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, from));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = plainTextBody };

        if (!string.IsNullOrWhiteSpace(replyTo))
            message.ReplyTo.Add(MailboxAddress.Parse(replyTo));

        return message;
    }

    private async Task SendAsync(MimeMessage message, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new SmtpClient();

            var secureOption = _settings.SmtpPort == 465
                ? SecureSocketOptions.SslOnConnect
                : SecureSocketOptions.StartTls;

            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, secureOption, cancellationToken);
            await client.AuthenticateAsync(_settings.SmtpUser, _settings.SmtpPassword, cancellationToken);
            await client.SendAsync(message, cancellationToken);
            await client.DisconnectAsync(true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail para {To}", message.To);
            throw;
        }
    }
}
