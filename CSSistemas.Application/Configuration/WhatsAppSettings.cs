namespace CSSistemas.Application.Configuration;

/// <summary>Configuração do WhatsApp (uazapi.dev). Preenchido via appsettings na API.</summary>
public class WhatsAppSettings
{
    public const string SectionName = "WhatsApp";

    /// <summary>Habilitar envio de mensagens via WhatsApp.</summary>
    public bool Enabled { get; set; }
    /// <summary>URL base da instância no uazapi.dev (ex: https://SUA-INSTANCIA.uazapi.dev).</summary>
    public string ApiUrl { get; set; } = string.Empty;
    /// <summary>Token de autenticação da instância no uazapi.dev.</summary>
    public string ApiToken { get; set; } = string.Empty;
}
