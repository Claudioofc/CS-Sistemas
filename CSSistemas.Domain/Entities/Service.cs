namespace CSSistemas.Domain.Entities;

/// <summary>Serviço oferecido por um negócio (ex: consulta, corte).</summary>
public class Service : EntityBase
{
    public Guid BusinessId { get; protected set; }
    public string Name { get; protected set; } = string.Empty;
    public int DurationMinutes { get; protected set; }
    public decimal? Price { get; protected set; }
    public bool IsActive { get; protected set; } = true;

    public Business Business { get; protected set; } = null!;
    public ICollection<Appointment> Appointments { get; protected set; } = new List<Appointment>();

    protected Service() { }

    public static Service Create(Guid businessId, string name, int durationMinutes, decimal? price = null)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId é obrigatório.", nameof(businessId));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do serviço é obrigatório.", nameof(name));
        if (durationMinutes <= 0)
            throw new ArgumentException("Duração deve ser maior que zero.", nameof(durationMinutes));
        if (price.HasValue && price.Value < 0)
            throw new ArgumentException("Preço não pode ser negativo.", nameof(price));

        return new Service
        {
            BusinessId = businessId,
            Name = name.Trim(),
            DurationMinutes = durationMinutes,
            Price = price
        };
    }

    public void Update(string name, int durationMinutes, decimal? price, bool isActive = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do serviço é obrigatório.", nameof(name));
        if (durationMinutes <= 0)
            throw new ArgumentException("Duração deve ser maior que zero.", nameof(durationMinutes));
        if (price.HasValue && price.Value < 0)
            throw new ArgumentException("Preço não pode ser negativo.", nameof(price));

        Name = name.Trim();
        DurationMinutes = durationMinutes;
        Price = price;
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }
}
