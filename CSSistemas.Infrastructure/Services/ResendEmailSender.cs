using System.Net.Http.Json;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Envio de e-mail via Resend (resend.com). Uma chave de API, sem senha de app. Mais prático para o cliente.</summary>
public class ResendEmailSender : IEmailSender
{
    private const string ResendApiUrl = "https://api.resend.com/emails";
    private readonly EmailSettings _settings;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(
        IOptions<EmailSettings> settings,
        IHttpClientFactory httpClientFactory,
        ILogger<ResendEmailSender> logger)
    {
        _settings = settings.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task SendPasswordResetAsync(string email, string resetLink, CancellationToken cancellationToken = default)
    {
        var apiKey = _settings.ResendApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Resend configurado mas ResendApiKey está vazio. Link: {Link}", resetLink);
            await Task.CompletedTask;
            return;
        }

        var from = string.IsNullOrWhiteSpace(_settings.FromEmail)
            ? $"CS Sistemas <onboarding@resend.dev>"
            : $"{_settings.FromName} <{_settings.FromEmail}>";

        var body = new
        {
            from,
            to = new[] { email },
            subject = "Redefinição de senha - CS Sistemas",
            html = $@"
<p>Você solicitou a redefinição de senha.</p>
<p>Clique no link abaixo para definir uma nova senha (válido por 1 hora):</p>
<p><a href=""{resetLink}"" style=""color:#2563eb;text-decoration:underline;"">Redefinir minha senha</a></p>
<p>Ou copie e cole no navegador:</p>
<p style=""word-break:break-all;color:#666;"">{resetLink}</p>
<p>Se você não solicitou isso, ignore este e-mail.</p>
<p>— CS Sistemas</p>"
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);

            var response = await client.PostAsJsonAsync(ResendApiUrl, body, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend API erro {StatusCode}: {Body}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"Falha ao enviar e-mail: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de redefinição para {Email} via Resend", email);
            throw;
        }
    }

    public async Task SendAppointmentConfirmationAsync(string toEmail, string clientName, string scheduledAtFormatted, string serviceName, string businessName, string cancelLink, CancellationToken cancellationToken = default)
    {
        var apiKey = _settings.ResendApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Resend configurado mas ResendApiKey vazio. Link cancelar: {Link}", cancelLink);
            await Task.CompletedTask;
            return;
        }

        var from = string.IsNullOrWhiteSpace(_settings.FromEmail)
            ? $"CS Sistemas <onboarding@resend.dev>"
            : $"{_settings.FromName} <{_settings.FromEmail}>";

        var body = new
        {
            from,
            to = new[] { toEmail },
            subject = "Agendamento confirmado - " + businessName,
            html = $@"
<p>Olá, {System.Net.WebUtility.HtmlEncode(clientName)}.</p>
<p>Seu agendamento em <strong>{System.Net.WebUtility.HtmlEncode(businessName)}</strong> foi confirmado.</p>
<p><strong>Serviço:</strong> {System.Net.WebUtility.HtmlEncode(serviceName)}<br/>
<strong>Data/hora:</strong> {System.Net.WebUtility.HtmlEncode(scheduledAtFormatted)}</p>
<p>Para cancelar, use o link abaixo:</p>
<p><a href=""{cancelLink}"" style=""color:#2563eb;text-decoration:underline;"">Cancelar este agendamento</a></p>
<p style=""word-break:break-all;color:#666;font-size:12px;"">{cancelLink}</p>
<p>— CS Sistemas</p>"
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
            var response = await client.PostAsJsonAsync(ResendApiUrl, body, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend API erro {StatusCode}: {Body}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"Falha ao enviar e-mail: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de confirmação para {Email} via Resend", toEmail);
            throw;
        }
    }

    public async Task SendAppointmentCancelledByProfessionalAsync(string toEmail, string clientName, string scheduledAtFormatted, string businessName, string? cancellationReason = null, CancellationToken cancellationToken = default)
    {
        var apiKey = _settings.ResendApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Resend configurado mas ResendApiKey vazio.");
            await Task.CompletedTask;
            return;
        }

        var from = string.IsNullOrWhiteSpace(_settings.FromEmail)
            ? $"CS Sistemas <onboarding@resend.dev>"
            : $"{_settings.FromName} <{_settings.FromEmail}>";

        var reasonBlock = !string.IsNullOrWhiteSpace(cancellationReason)
            ? $"<p><strong>Motivo informado:</strong> {System.Net.WebUtility.HtmlEncode(cancellationReason.Trim())}</p>"
            : "";
        var body = new
        {
            from,
            to = new[] { toEmail },
            subject = "Agendamento cancelado - " + businessName,
            html = $@"
<p>Olá, {System.Net.WebUtility.HtmlEncode(clientName)}.</p>
<p>Infelizmente seu agendamento em <strong>{System.Net.WebUtility.HtmlEncode(businessName)}</strong> para o dia <strong>{System.Net.WebUtility.HtmlEncode(scheduledAtFormatted)}</strong> foi cancelado.</p>
{reasonBlock}
<p>Se quiser reagendar, entre em contato com o estabelecimento.</p>
<p>— CS Sistemas</p>"
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
            var response = await client.PostAsJsonAsync(ResendApiUrl, body, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend API erro {StatusCode}: {Body}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"Falha ao enviar e-mail: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de cancelamento para {Email} via Resend", toEmail);
            throw;
        }
    }

    public async Task SendNewUserRegisteredAsync(string toEmail, string newUserName, string newUserEmail, CancellationToken cancellationToken = default)
    {
        var apiKey = _settings.ResendApiKey?.Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Resend configurado mas ResendApiKey vazio. Novo usuário: {Name} ({Email})", newUserName, newUserEmail);
            await Task.CompletedTask;
            return;
        }

        var from = string.IsNullOrWhiteSpace(_settings.FromEmail)
            ? $"CS Sistemas <onboarding@resend.dev>"
            : $"{_settings.FromName} <{_settings.FromEmail}>";

        var body = new
        {
            from,
            to = new[] { toEmail },
            subject = "Novo cadastro - CS Sistemas",
            html = $@"
<p>Um novo usuário se cadastrou no sistema.</p>
<p><strong>Nome:</strong> {System.Net.WebUtility.HtmlEncode(newUserName)}<br/>
<strong>E-mail:</strong> {System.Net.WebUtility.HtmlEncode(newUserEmail)}</p>
<p>— CS Sistemas</p>"
        };

        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", "Bearer " + apiKey);
            var response = await client.PostAsJsonAsync(ResendApiUrl, body, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Resend API erro {StatusCode}: {Body}", response.StatusCode, errorBody);
                throw new InvalidOperationException($"Falha ao enviar e-mail: {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Falha ao enviar e-mail de novo cadastro para {Email} via Resend", toEmail);
            throw;
        }
    }
}
