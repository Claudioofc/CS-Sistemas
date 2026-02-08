namespace CSSistemas.Domain.Entities;

/// <summary>Cliente/paciente de um negócio (ex.: paciente da clínica odontológica).</summary>
public class Client : EntityBase
{
    public Guid BusinessId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public string? Phone { get; protected set; }
    public string? Email { get; protected set; }
    public string? Notes { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    public Business Business { get; protected set; } = null!;

    protected Client() { }

    public static Client Create(Guid businessId, string name, string? phone = null, string? email = null, string? notes = null)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId é obrigatório.", nameof(businessId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do cliente é obrigatório.", nameof(name));

        return new Client
        {
            BusinessId = businessId,
            Name = name.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public void Update(string name, string? phone, string? email, string? notes, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do cliente é obrigatório.", nameof(name));
        Name = name.Trim();
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();
        Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
