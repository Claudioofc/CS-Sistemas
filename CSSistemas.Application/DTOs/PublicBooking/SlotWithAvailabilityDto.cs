namespace CSSistemas.Application.DTOs.PublicBooking;

/// <summary>Slot de horário com indicação se está disponível ou ocupado.</summary>
public record SlotWithAvailabilityDto(DateTime ScheduledAtUtc, bool Available);
