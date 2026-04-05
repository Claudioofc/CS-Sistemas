namespace CSSistemas.Domain.Entities;

/// <summary>Preço personalizado de um funcionário para um serviço específico. Sobrepõe o preço padrão do serviço.</summary>
public class EmployeeServicePrice
{
    public Guid EmployeeId { get; private set; }
    public Guid ServiceId { get; private set; }
    /// <summary>Preço cobrado por este funcionário para este serviço.</summary>
    public decimal Price { get; private set; }

    protected EmployeeServicePrice() { }

    public static EmployeeServicePrice Create(Guid employeeId, Guid serviceId, decimal price)
    {
        if (employeeId == Guid.Empty) throw new ArgumentException("EmployeeId é obrigatório.", nameof(employeeId));
        if (serviceId == Guid.Empty) throw new ArgumentException("ServiceId é obrigatório.", nameof(serviceId));
        if (price < 0) throw new ArgumentException("Preço não pode ser negativo.", nameof(price));
        return new EmployeeServicePrice { EmployeeId = employeeId, ServiceId = serviceId, Price = price };
    }
}
