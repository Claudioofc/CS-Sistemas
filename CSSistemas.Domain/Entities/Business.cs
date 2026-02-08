using CSSistemas.Domain.Enums;

namespace CSSistemas.Domain.Entities;

/// <summary>Negócio do cliente SaaS. Um usuário pode ter um ou mais negócios.</summary>
public class Business : EntityBase
{
    public Guid UserId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public BusinessType BusinessType { get; protected set; }
    public string? PublicSlug { get; protected set; }
    /// <summary>Número WhatsApp do negócio (ex: 5511999999999) para receber webhook e enviar respostas.</summary>
    public string? WhatsAppPhone { get; protected set; }

    public User User { get; protected set; } = null!;
    public ICollection<Service> Services { get; protected set; } = new List<Service>();
    public ICollection<Appointment> Appointments { get; protected set; } = new List<Appointment>();
    public ICollection<Client> Clients { get; protected set; } = new List<Client>();
    public ICollection<SystemMessage> SystemMessages { get; protected set; } = new List<SystemMessage>();
    public ICollection<BusinessHours> BusinessHours { get; protected set; } = new List<BusinessHours>();

    protected Business() { }

    public static Business Create(Guid userId, string name, BusinessType businessType, string? publicSlug = null, string? whatsAppPhone = null)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId é obrigatório.", nameof(userId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do negócio é obrigatório.", nameof(name));

        return new Business
        {
            UserId = userId,
            Name = name.Trim(),
            BusinessType = businessType,
            PublicSlug = string.IsNullOrWhiteSpace(publicSlug) ? null : publicSlug.Trim().ToLowerInvariant(),
            WhatsAppPhone = string.IsNullOrWhiteSpace(whatsAppPhone) ? null : new string(whatsAppPhone.Trim().Where(char.IsDigit).ToArray())
        };
    }

    public void Update(string name, BusinessType businessType, string? publicSlug = null, string? whatsAppPhone = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do negócio é obrigatório.", nameof(name));
        Name = name.Trim();
        BusinessType = businessType;
        PublicSlug = string.IsNullOrWhiteSpace(publicSlug) ? null : publicSlug.Trim().ToLowerInvariant();
        WhatsAppPhone = string.IsNullOrWhiteSpace(whatsAppPhone) ? null : new string(whatsAppPhone.Trim().Where(char.IsDigit).ToArray());
        UpdatedAt = DateTime.UtcNow;
    }
}
