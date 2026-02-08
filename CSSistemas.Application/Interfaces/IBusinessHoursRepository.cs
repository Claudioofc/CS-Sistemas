using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface IBusinessHoursRepository
{
    Task<IReadOnlyList<BusinessHours>> GetByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
    /// <summary>Obtém horários do negócio com rastreamento (para atualização).</summary>
    Task<List<BusinessHours>> GetByBusinessIdForUpdateAsync(Guid businessId, CancellationToken cancellationToken = default);
    Task<BusinessHours?> GetByBusinessIdAndDayAsync(Guid businessId, int dayOfWeek, CancellationToken cancellationToken = default);
    Task AddAsync(BusinessHours entity, CancellationToken cancellationToken = default);
    Task UpdateAsync(BusinessHours entity, CancellationToken cancellationToken = default);
    Task DeleteByBusinessIdAsync(Guid businessId, CancellationToken cancellationToken = default);
}
