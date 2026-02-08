using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.Interfaces;

/// <summary>Repositório de assinaturas (trial/plano pago).</summary>
public interface ISubscriptionRepository
{
    /// <summary>Retorna a assinatura ativa do usuário (trial ou paga) cujo EndsAt >= agora, ou null.</summary>
    Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Indica se o usuário tem alguma assinatura (para backfill de trial em usuários antigos).</summary>
    Task<bool> ExistsAnyByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Retorna a assinatura ativa de cada usuário (para painel admin). Um por UserId, a mais recente por usuário.</summary>
    Task<IReadOnlyList<Subscription>> GetActiveByUserIdsAsync(IEnumerable<Guid> userIds, CancellationToken cancellationToken = default);

    /// <summary>Lista assinaturas premium (Monthly) com User, ordenadas por StartedAt desc. Apenas admin.</summary>
    Task<IReadOnlyList<Subscription>> GetPremiumSubscriptionsOrderedByStartedAtAsync(int limit = 100, CancellationToken cancellationToken = default);

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
