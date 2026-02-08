using System.Collections.Concurrent;
using CSSistemas.Application.Interfaces;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Store em memória para slots pendentes (uma instância). Para múltiplas instâncias use Redis.</summary>
public sealed class InMemoryPendingWhatsAppSlotStore : IPendingWhatsAppSlotStore
{
    private static readonly ConcurrentDictionary<string, (PendingSlotData Data, DateTimeOffset? Expiry)> Store = new();

    public Task SetAsync(string phoneNormalized, PendingSlotData data, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var expiryAt = expiry.HasValue ? DateTimeOffset.UtcNow.Add(expiry.Value) : (DateTimeOffset?)null;
        Store[phoneNormalized] = (data, expiryAt);
        return Task.CompletedTask;
    }

    public Task<PendingSlotData?> TryGetAndRemoveAsync(string phoneNormalized, CancellationToken cancellationToken = default)
    {
        if (!Store.TryRemove(phoneNormalized, out var entry))
            return Task.FromResult<PendingSlotData?>(null);
        if (entry.Expiry.HasValue && entry.Expiry.Value < DateTimeOffset.UtcNow)
            return Task.FromResult<PendingSlotData?>(null);
        return Task.FromResult<PendingSlotData?>(entry.Data);
    }
}
