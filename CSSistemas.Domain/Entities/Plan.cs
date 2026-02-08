namespace CSSistemas.Domain.Entities;

/// <summary>
/// Plano de assinatura (mensal, 6 meses, 1 ano). Valores e intervalo de cobrança.
/// </summary>
public class Plan
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public string Name { get; protected set; } = string.Empty;
    public decimal Price { get; protected set; }
    /// <summary>Intervalo de cobrança em meses: 1 = mensal, 6 = 6 meses, 12 = 1 ano.</summary>
    public int BillingIntervalMonths { get; protected set; }
    public string? Features { get; protected set; }
    public bool IsActive { get; protected set; } = true;
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;

    protected Plan() { }

    public static Plan Create(string name, decimal price, int billingIntervalMonths, string? features = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nome do plano é obrigatório.", nameof(name));
        if (price <= 0)
            throw new ArgumentException("Preço deve ser maior que zero.", nameof(price));
        if (billingIntervalMonths < 1)
            throw new ArgumentException("Intervalo deve ser pelo menos 1 mês.", nameof(billingIntervalMonths));

        return new Plan
        {
            Name = name.Trim(),
            Price = price,
            BillingIntervalMonths = billingIntervalMonths,
            Features = string.IsNullOrWhiteSpace(features) ? null : features.Trim(),
            IsActive = true
        };
    }
}
