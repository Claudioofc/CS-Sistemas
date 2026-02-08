using CSSistemas.Domain.Entities;

namespace CSSistemas.Application.Interfaces;

/// <summary>Repositório de planos de assinatura.</summary>
public interface IPlanRepository
{
    /// <summary>Lista planos ativos, ordenados por intervalo (mensal primeiro).</summary>
    Task<IReadOnlyList<Plan>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<Plan?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
}
