using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Employee?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Employee>> GetByBusinessIdAsync(Guid businessId, bool onlyActive = false, CancellationToken cancellationToken = default);
    Task<int> CountActiveByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task AddAsync(Employee employee, CancellationToken cancellationToken = default);
    Task UpdateAsync(Employee employee, CancellationToken cancellationToken = default);
    Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
}
