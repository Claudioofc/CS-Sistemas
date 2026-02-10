using System.IO;
using System.Net;
using System.Net.Mail;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Envio de e-mail. Se SMTP não estiver configurado, apenas loga o link (desenvolvimento).</summary>
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
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation("E-mail não configurado. Link de redefinição para {Email}: {Link}", email, resetLink);
            await Task.CompletedTask;
            return;
        }

        var smtpUser = _settings.SmtpUser?.Trim() ?? "";
        var smtpPassword = _settings.SmtpPassword ?? "";

        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogWarning("SMTP configurado mas usuário ou senha está vazio. Use senha de app (Gmail/Yahoo). Link: {Link}", resetLink);
            await Task.CompletedTask;
            return;
        }

        try
        {
            // Gmail e outros exigem TLS 1.2+
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpPort == 587 || _settings.SmtpPort == 465,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };

            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? smtpUser : _settings.FromEmail.Trim();
            var mail = new MailMessage(from, email, "Redefinição de senha - CS Sistemas",
                $"Clique no link abaixo para redefinir sua senha (válido por 1 hora):\n\n{resetLink}\n\nSe você não solicitou isso, ignore este e-mail.");
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de redefinição para {Email}", email);
            throw;
        }
    }

    public async Task SendAppointmentConfirmationAsync(string toEmail, string clientName, string scheduledAtFormatted, string serviceName, string businessName, string cancelLink, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation("E-mail não configurado. Confirmação para {Email}: {BusinessName}, {ScheduledAt}. Link cancelar: {Link}", toEmail, businessName, scheduledAtFormatted, cancelLink);
            await Task.CompletedTask;
            return;
        }

        var smtpUser = _settings.SmtpUser?.Trim() ?? "";
        var smtpPassword = _settings.SmtpPassword ?? "";
        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            _logger.LogWarning("SMTP configurado mas usuário ou senha vazio. Link cancelar: {Link}", cancelLink);
            await Task.CompletedTask;
            return;
        }

        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpPort == 587 || _settings.SmtpPort == 465,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };
            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? smtpUser : _settings.FromEmail.Trim();
            var body = $"Olá, {clientName}.\n\nSeu agendamento em {businessName} foi confirmado.\n\nServiço: {serviceName}\nData/hora: {scheduledAtFormatted}\n\nPara cancelar, use o link abaixo:\n{cancelLink}\n\n— CS Sistemas";
            var mail = new MailMessage(from, toEmail, "Agendamento confirmado - " + businessName, body);
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de confirmação para {Email}", toEmail);
            throw;
        }
    }

    public async Task SendAppointmentCancelledByProfessionalAsync(string toEmail, string clientName, string scheduledAtFormatted, string businessName, string? cancellationReason = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation("E-mail não configurado. Cancelamento para {Email}: {BusinessName}, {ScheduledAt}", toEmail, businessName, scheduledAtFormatted);
            await Task.CompletedTask;
            return;
        }

        var smtpUser = _settings.SmtpUser?.Trim() ?? "";
        var smtpPassword = _settings.SmtpPassword ?? "";
        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpPort == 587 || _settings.SmtpPort == 465,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };
            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? smtpUser : _settings.FromEmail.Trim();
            var reasonBlock = !string.IsNullOrWhiteSpace(cancellationReason)
                ? $"\nMotivo informado: {cancellationReason.Trim()}\n\n"
                : "\n\n";
            var body = $"Olá, {clientName}.\n\nInfelizmente seu agendamento em {businessName} para o dia {scheduledAtFormatted} foi cancelado.{reasonBlock}Se quiser reagendar, entre em contato com o estabelecimento.\n\n— CS Sistemas";
            var mail = new MailMessage(from, toEmail, "Agendamento cancelado - " + businessName, body);
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de cancelamento para {Email}", toEmail);
            throw;
        }
    }

    public async Task SendNewUserRegisteredAsync(string toEmail, string newUserName, string newUserEmail, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation("E-mail não configurado. Novo usuário: {Name} ({Email})", newUserName, newUserEmail);
            await Task.CompletedTask;
            return;
        }

        var smtpUser = _settings.SmtpUser?.Trim() ?? "";
        var smtpPassword = _settings.SmtpPassword ?? "";
        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpPort == 587 || _settings.SmtpPort == 465,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };
            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? smtpUser : _settings.FromEmail.Trim();
            var body = $"Um novo usuário se cadastrou no sistema.\n\nNome: {newUserName}\nE-mail: {newUserEmail}\n\n— CS Sistemas";
            var mail = new MailMessage(from, toEmail, "Novo cadastro - CS Sistemas", body);
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de novo cadastro para {Email}", toEmail);
            throw;
        }
    }

    public async Task SendWelcomeToNewUserAsync(string toEmail, string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation("E-mail não configurado. Boas-vindas para: {Name} ({Email})", userName, toEmail);
            await Task.CompletedTask;
            return;
        }

        var smtpUser = _settings.SmtpUser?.Trim() ?? "";
        var smtpPassword = _settings.SmtpPassword ?? "";
        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpPort == 587 || _settings.SmtpPort == 465,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };
            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? smtpUser : _settings.FromEmail.Trim();
            var body = WelcomeEmailContent.BuildPlainTextBody(userName);
            var mail = new MailMessage(from, toEmail, WelcomeEmailContent.Subject, body);
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de boas-vindas para {Email}", toEmail);
            throw;
        }
    }

    public async Task SendSupportRequestAsync(string toEmail, string userName, string userEmail, string message, string? pageUrl = null, byte[]? attachment = null, string? attachmentFileName = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogInformation("E-mail não configurado. Suporte: {Name} ({Email}): {Message}", userName, userEmail, message);
            await Task.CompletedTask;
            return;
        }

        var smtpUser = _settings.SmtpUser?.Trim() ?? "";
        var smtpPassword = _settings.SmtpPassword ?? "";
        if (string.IsNullOrEmpty(smtpUser) || string.IsNullOrEmpty(smtpPassword))
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                EnableSsl = _settings.SmtpPort == 587 || _settings.SmtpPort == 465,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(smtpUser, smtpPassword)
            };
            var from = string.IsNullOrWhiteSpace(_settings.FromEmail) ? smtpUser : _settings.FromEmail.Trim();
            var body = SupportRequestEmailContent.BuildPlainTextBody(userName, userEmail, message, pageUrl);
            var mail = new MailMessage(from, toEmail, SupportRequestEmailContent.Subject, body);
            mail.BodyEncoding = System.Text.Encoding.UTF8;
            if (attachment != null && !string.IsNullOrWhiteSpace(attachmentFileName))
            {
                using var attachmentStream = new MemoryStream(attachment);
                mail.Attachments.Add(new Attachment(attachmentStream, attachmentFileName));
            }
            await client.SendMailAsync(mail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de suporte para {Email}", toEmail);
            throw;
        }
    }
}
