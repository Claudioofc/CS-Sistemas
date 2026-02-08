namespace CSSistemas.Application.Configuration;

/// <summary>Configuração do WhatsApp (Z-API / Twilio / Meta). Preenchido via appsettings na API.</summary>
public class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    /// <summary>Habilitar envio de mensagens via WhatsApp.</summary>
    public bool Enabled { get; set; }
    /// <summary>URL base da API (ex: Z-API https://api.z-api.io).</summary>
    public string ApiUrl { get; set; } = string.Empty;
    /// <summary>Token ou Instance/Token para envio.</summary>
    public string ApiToken { get; set; } = string.Empty;
    /// <summary>Formato: Z-API = "Z-API", Twilio = "Twilio", Meta = "Meta".</summary>
    public string Provider { get; set; } = "Z-API";
}
