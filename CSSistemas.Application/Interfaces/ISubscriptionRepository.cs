using CSSistemas.Domain.Entities;

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

    Task AddAsync(Subscription subscription, CancellationToken cancellationToken = default);
}
