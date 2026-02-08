namespace CSSistemas.Application.Configuration;

/// <summary>Configuração do administrador (notificações por e-mail em eventos do sistema).</summary>
public class AdminSettings
{
    public const string SectionName = "Admin";
    /// <summary>E-mail para receber avisos (ex.: novo usuário cadastrado). Se vazio, não envia e-mail.</summary>
    public string? NotificationEmail { get; set; }
}
