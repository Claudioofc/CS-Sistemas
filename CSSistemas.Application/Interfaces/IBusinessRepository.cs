using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

public interface IBusinessRepository
{
    Task<Business?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Business?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    /// <summary>Obtém entidade rastreada para atualização.</summary>
    Task<Business?> GetByIdAndUserIdForUpdateAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Business>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>Lista todos os negócios (uso admin). Inclui User (dono) para exibir nome.</summary>
    Task<IReadOnlyList<Business>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Business?> GetByPublicSlugAsync(string publicSlug, CancellationToken cancellationToken = default);
    Task<Business?> GetByWhatsAppPhoneAsync(string phoneNormalized, CancellationToken cancellationToken = default);
    Task AddAsync(Business business, CancellationToken cancellationToken = default);
    Task UpdateAsync(Business business, CancellationToken cancellationToken = default);
    /// <summary>Soft delete: marca como excluído (não remove do banco).</summary>
    Task<bool> SoftDeleteAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
}
