namespace CSSistemas.Domain.Entities;

/// <summary>Funcionário/profissional de um negócio. Cada agendamento pode ser atribuído a um funcionário.</summary>
public class Employee : EntityBase
{
    public Guid BusinessId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    /// <summary>Cargo ou especialidade (ex.: "Barbeiro", "Manicure"). Opcional.</summary>
    public string? Role { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    public Business Business { get; protected set; } = null!;
    public ICollection<Appointment> Appointments { get; protected set; } = new List<Appointment>();

    protected Employee() { }

    public static Employee Create(Guid businessId, string name, string? role = null)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId é obrigatório.", nameof(businessId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));

        return new Employee
        {
            BusinessId = businessId,
            Name = name.Trim(),
            Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim(),
            IsActive = true
        };
    }

    public void Update(string name, string? role, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome é obrigatório.", nameof(name));
        Name = name.Trim();
        Role = string.IsNullOrWhiteSpace(role) ? null : role.Trim();
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
