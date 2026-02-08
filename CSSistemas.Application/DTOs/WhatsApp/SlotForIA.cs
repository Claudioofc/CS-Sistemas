namespace CSSistemas.Application.DTOs.WhatsApp;

/// <summary>Slot disponível passado para a IA sugerir (serviceId, nome do serviço, horário UTC em ISO).</summary>
public record SlotForIA(Guid ServiceId, string ServiceName, DateTime ScheduledAtUtc);
