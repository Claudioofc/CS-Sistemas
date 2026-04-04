using System.Net.Http.Json;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Envio de mensagem WhatsApp via uazapi.dev.</summary>
public class UazApiWhatsAppSender : IWhatsAppSender
{
    private readonly WhatsAppSettings _settings;
    private readonly HttpClient _httpClient;
    private readonly ILogger<UazApiWhatsAppSender> _logger;

    public UazApiWhatsAppSender(IOptions<WhatsAppSettings> settings, HttpClient httpClient, ILogger<UazApiWhatsAppSender> logger)
    {
        _settings = settings.Value;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[WhatsApp] Envio desabilitado. Para: {Phone}", phoneNumber);
            return true;
        }

        if (string.IsNullOrWhiteSpace(_settings.ApiUrl) || string.IsNullOrWhiteSpace(_settings.ApiToken))
        {
            _logger.LogWarning("[WhatsApp] ApiUrl ou ApiToken não configurados.");
            return false;
        }

        try
        {
            var payload = new { number = phoneNumber, text };
            var request = new HttpRequestMessage(HttpMethod.Post, $"{_settings.ApiUrl.TrimEnd('/')}/send-text")
            {
                Content = JsonContent.Create(payload)
            };
            request.Headers.Add("token", _settings.ApiToken);

            var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning("[WhatsApp] Falha ao enviar para {Phone}. Status: {Status}. Body: {Body}", phoneNumber, response.StatusCode, body);
                return false;
            }

            _logger.LogInformation("[WhatsApp] Mensagem enviada para {Phone}.", phoneNumber);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[WhatsApp] Erro ao enviar mensagem para {Phone}.", phoneNumber);
            return false;
        }
    }
}
