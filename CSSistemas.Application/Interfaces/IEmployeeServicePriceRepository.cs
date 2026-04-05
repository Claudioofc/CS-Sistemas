using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface IEmployeeServicePriceRepository
{
    /// <summary>Retorna todos os preços cadastrados para os funcionários informados.</summary>
    Task<IReadOnlyList<EmployeeServicePrice>> GetByEmployeeIdsAsync(IEnumerable<Guid> employeeIds, CancellationToken cancellationToken = default);

    /// <summary>Substitui todos os preços do funcionário pelos novos (delete + insert).</summary>
    Task ReplaceAllForEmployeeAsync(Guid employeeId, IEnumerable<(Guid ServiceId, decimal Price)> prices, CancellationToken cancellationToken = default);

    /// <summary>Substitui os preços de todos os funcionários para um serviço específico (delete + insert).</summary>
    Task ReplaceAllForServiceAsync(Guid serviceId, Guid businessId, IEnumerable<(Guid EmployeeId, decimal Price)> prices, CancellationToken cancellationToken = default);
}
