using CSSistemas.Domain.Enums;

namespace CSSistemas.Domain.Entities;

/// <summary>
/// Usuário do sistema (cliente do SaaS). Administra um ou mais negócios.
/// </summary>
public class User : EntityBase
{
    public string Email { get; protected set; } = string.Empty;
    public string PasswordHash { get; protected set; } = string.Empty;
    public string Name { get; protected set; } = string.Empty;
    public string? ProfilePhotoUrl { get; protected set; }
    public DocumentType? DocumentType { get; protected set; }
    public string? DocumentNumber { get; protected set; }
    public string? ResetToken { get; protected set; }
    public DateTime? ResetTokenExpiresAt { get; protected set; }
    /// <summary>Indica se o usuário é administrador do sistema (dono do SaaS).</summary>
    public bool IsAdmin { get; protected set; }
    /// <summary>Quantidade de tentativas de login falhas seguidas.</summary>
    public int FailedLoginAttempts { get; protected set; }
    /// <summary>Fim do bloqueio por excesso de tentativas (UTC).</summary>
    public DateTime? LockoutEnd { get; protected set; }

    protected User() { }

    /// <summary>Define o usuário como administrador do sistema.</summary>
    public void SetAsAdmin()
    {
        IsAdmin = true;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Cria usuário. documentNumber deve conter apenas dígitos (11 CPF ou 14 CNPJ).</summary>
    public static User Create(string email, string passwordHash, string name, DocumentType? documentType = null, string? documentNumber = null)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email é obrigatório.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Senha é obrigatória.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));

        var user = new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            Name = name.Trim()
        };
        if (documentType.HasValue && !string.IsNullOrWhiteSpace(documentNumber))
        {
            user.DocumentType = documentType;
            user.DocumentNumber = documentNumber.Trim();
        }
        return user;
    }

    /// <summary>Define token para redefinição de senha (esqueci minha senha).</summary>
    public void SetPasswordResetToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token é obrigatório.", nameof(token));
        ResetToken = token;
        ResetTokenExpiresAt = expiresAt;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Registra falha de login. Após 3 falhas, bloqueia até redefinir senha por e-mail.</summary>
    public void RecordFailedLogin()
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= 3)
            LockoutEnd = DateTime.MaxValue; // Bloqueio indefinido; só desbloqueia ao redefinir senha
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Zera tentativas e bloqueio (login correto ou após redefinir senha).</summary>
    public void ResetFailedLogins()
    {
        FailedLoginAttempts = 0;
        LockoutEnd = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Limpa token após redefinir senha ou expiração.</summary>
    public void ClearPasswordReset()
    {
        ResetToken = null;
        ResetTokenExpiresAt = null;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Atualiza senha (hash). Usado na redefinição.</summary>
    public void SetPassword(string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("Senha é obrigatória.", nameof(passwordHash));
        PasswordHash = passwordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Atualiza nome, foto de perfil e/ou documento (CPF/CNPJ).</summary>
    public void UpdateProfile(string name, string? profilePhotoUrl = null, DocumentType? documentType = null, string? documentNumber = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));
        Name = name.Trim();
        ProfilePhotoUrl = string.IsNullOrWhiteSpace(profilePhotoUrl) ? null : profilePhotoUrl.Trim();
        if (documentType.HasValue && !string.IsNullOrWhiteSpace(documentNumber))
        {
            DocumentType = documentType;
            DocumentNumber = documentNumber.Trim();
        }
        else
        {
            DocumentType = null;
            DocumentNumber = null;
        }
        UpdatedAt = DateTime.UtcNow;
    }
}
