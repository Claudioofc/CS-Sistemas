namespace CSSistemas.Application.Configuration;

/// <summary>Configuração da OpenAI (IA conversacional). Preenchido via appsettings na API.</summary>
public class OpenAISettings
{
    public const string SectionName = "OpenAI";

    /// <summary>API Key da OpenAI. Nunca exponha no frontend.</summary>
    public string ApiKey { get; set; } = string.Empty;
    /// <summary>Modelo (ex: gpt-4o-mini, gpt-4o).</summary>
    public string Model { get; set; } = "gpt-4o-mini";
    /// <summary>Habilitar integração com IA (se false, responde mensagem padrão).</summary>
    public bool Enabled { get; set; }
}
