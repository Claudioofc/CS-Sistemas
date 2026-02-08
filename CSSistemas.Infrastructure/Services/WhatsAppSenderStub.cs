using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Implementação de envio WhatsApp (stub: apenas loga; configure Z-API/Twilio/Meta para envio real).</summary>
public class WhatsAppSenderStub : IWhatsAppSender
{
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppSenderStub> _logger;

    public WhatsAppSenderStub(IOptions<WhatsAppSettings> settings, ILogger<WhatsAppSenderStub> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("[WhatsApp STUB] Envio desabilitado. Para: {Phone}, Texto: {Text}", phoneNumber, text);
            return Task.FromResult(true);
        }
        _logger.LogInformation("[WhatsApp STUB] Enviaria para {Phone}: {Text}. Configure Provider ({Provider}) para envio real.", phoneNumber, text, _settings.Provider);
        return Task.FromResult(true);
    }
}
