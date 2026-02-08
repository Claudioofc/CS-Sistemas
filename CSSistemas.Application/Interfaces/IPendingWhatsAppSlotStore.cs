namespace CSSistemas.Application.Interfaces;

/// <summary>Armazena slots pendentes de confirmação por WhatsApp. Usar implementação distribuída (ex.: Redis) para múltiplas instâncias.</summary>
public interface IPendingWhatsAppSlotStore
{
    Task SetAsync(string phoneNormalized, PendingSlotData data, TimeSpan? expiry = null, CancellationToken cancellationToken = default);
    Task<PendingSlotData?> TryGetAndRemoveAsync(string phoneNormalized, CancellationToken cancellationToken = default);
}

/// <summary>Dados do slot pendente (serializável para Redis/cache).</summary>
public record PendingSlotData(Guid BusinessId, Guid ServiceId, DateTime ScheduledAtUtc, string ClientPhone);
