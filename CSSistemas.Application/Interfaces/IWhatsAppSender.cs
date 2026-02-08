namespace CSSistemas.Application.Interfaces;

/// <summary>Envia mensagem de texto via WhatsApp (Z-API / Twilio / Meta).</summary>
public interface IWhatsAppSender
{
    /// <summary>Envia mensagem de texto para o número (formato: 5511999999999).</summary>
    Task<bool> SendTextAsync(string phoneNumber, string text, CancellationToken cancellationToken = default);
}
