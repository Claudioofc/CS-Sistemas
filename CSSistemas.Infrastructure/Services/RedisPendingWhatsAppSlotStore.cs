using System.Text.Json;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.Caching.Distributed;

namespace CSSistemas.Infrastructure.Services;

/// <summary>Store em Redis para slots pendentes (múltiplas instâncias).</summary>
public sealed class RedisPendingWhatsAppSlotStore : IPendingWhatsAppSlotStore
{
    private const string KeyPrefix = "whatsapp:pending:";
    private static readonly TimeSpan DefaultExpiry = TimeSpan.FromHours(1);
    private readonly IDistributedCache _cache;

    public RedisPendingWhatsAppSlotStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SetAsync(string phoneNormalized, PendingSlotData data, TimeSpan? expiry = null, CancellationToken cancellationToken = default)
    {
        var key = KeyPrefix + phoneNormalized;
        var json = JsonSerializer.Serialize(data);
        var options = new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = expiry ?? DefaultExpiry };
        await _cache.SetStringAsync(key, json, options, cancellationToken);
    }

    public async Task<PendingSlotData?> TryGetAndRemoveAsync(string phoneNormalized, CancellationToken cancellationToken = default)
    {
        var key = KeyPrefix + phoneNormalized;
        var json = await _cache.GetStringAsync(key, cancellationToken);
        if (string.IsNullOrEmpty(json)) return null;
        await _cache.RemoveAsync(key, cancellationToken);
        return JsonSerializer.Deserialize<PendingSlotData>(json);
    }
}
