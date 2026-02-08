using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface ISystemMessageRepository
{
    Task<SystemMessage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SystemMessage?> GetByIdAndBusinessIdAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
    Task<SystemMessage?> GetByIdAndBusinessIdForUpdateAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
    Task<SystemMessage?> GetByBusinessIdAndKeyAsync(Guid businessId, string key, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SystemMessage>> GetByBusinessIdAsync(Guid businessId, bool onlyActive = true, CancellationToken cancellationToken = default);
    Task AddAsync(SystemMessage message, CancellationToken cancellationToken = default);
    Task UpdateAsync(SystemMessage message, CancellationToken cancellationToken = default);
    /// <summary>Soft delete: marca como excluído (não remove do banco).</summary>
    Task<bool> SoftDeleteAsync(Guid id, Guid businessId, CancellationToken cancellationToken = default);
}
