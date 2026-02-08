using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface IServiceRepository
{
    Task<Service?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Service?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
    Task<Service?> GetByIdAndBusinessIdForUpdateAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Service>> GetByBusinessIdAsync(Guid businessId, bool onlyActive = true, CancellationToken cancellationToken = default);
    Task AddAsync(Service service, CancellationToken cancellationToken = default);
    Task UpdateAsync(Service service, CancellationToken cancellationToken = default);
    /// <summary>Soft delete: marca como excluído (não remove do banco).</summary>
    Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
}
