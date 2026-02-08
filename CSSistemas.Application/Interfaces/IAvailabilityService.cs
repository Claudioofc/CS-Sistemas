using CSSistemas.Application.DTOs.PublicBooking;

namespace CSSistemas.Application.Interfaces;

/// <summary>Calcula horários disponíveis para agendamento.</summary>
public interface IAvailabilityService
{
    /// <summary>Retorna slots de início disponíveis (UTC) para o negócio, serviço e data. Assume fuso Brasil (UTC-3) para a data.</summary>
    Task<IReadOnlyList<DateTime>> GetAvailableSlotsAsync(Guid businessId, Guid serviceId, DateTime date, int slotIntervalMinutes = 30, CancellationToken cancellationToken = default);

    /// <summary>Retorna todos os slots do dia (dentro do horário de funcionamento) com flag disponível/ocupado. Para exibir ocupados em vermelho na UI.</summary>
    Task<IReadOnlyList<SlotWithAvailabilityDto>> GetSlotsWithAvailabilityAsync(Guid businessId, Guid serviceId, DateTime date, int slotIntervalMinutes = 30, CancellationToken cancellationToken = default);
}
