namespace CSSistemas.Application.DTOs.WhatsApp;

/// <summary>Resposta da IA no fluxo WhatsApp: mensagem e opcionalmente um slot sugerido para agendamento.</summary>
public record WhatsAppIAResponse(string Message, SuggestedSlotDto? SuggestedSlot = null);

/// <summary>Slot sugerido pela IA para o cliente confirmar (serviceId + horário UTC).</summary>
public record SuggestedSlotDto(Guid ServiceId, DateTime ScheduledAtUtc);
