namespace CSSistemas.Application.Configuration;

/// <summary>Configuração de e-mail (redefinição de senha). Preenchido via appsettings na API.</summary>
public class EmailSettings
{
    public const string SectionName = "Email";
    /// <summary>Provedor: "Smtp" (Gmail/Yahoo/Outlook) ou "Resend" (uma chave de API). Resend é mais prático (sem senha de app).</summary>
    public string Provider { get; set; } = "Smtp";
    /// <summary>URL base do frontend para montar o link de redefinição (ex: https://app.cssistemas.com ou https://localhost:5173).</summary>
    public string PasswordResetBaseUrl { get; set; } = string.Empty;
    /// <summary>Chave de API do Resend (resend.com). Só usado se Provider = "Resend".</summary>
    public string? ResendApiKey { get; set; }
    /// <summary>SMTP opcional. Se vazio e Provider=Smtp, o link é apenas logado (desenvolvimento).</summary>
    public string? SmtpHost { get; set; }
    public int SmtpPort { get; set; } = 587;
    public string? SmtpUser { get; set; }
    public string? SmtpPassword { get; set; }
    public string? FromEmail { get; set; }
    public string FromName { get; set; } = "CS Sistemas";
}
