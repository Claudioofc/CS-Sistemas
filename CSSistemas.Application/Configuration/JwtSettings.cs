namespace CSSistemas.Application.Configuration;

/// <summary>
/// Configuração JWT (DRY). Preenchido via appsettings na API.
/// </summary>
public class JwtSettings
{
    public const string SectionName = "Jwt";
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int ExpiresMinutes { get; set; } = 60;
}
