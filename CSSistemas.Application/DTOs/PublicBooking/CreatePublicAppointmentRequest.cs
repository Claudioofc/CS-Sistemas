namespace CSSistemas.Application.DTOs.PublicBooking;

/// <summary>Request para criar agendamento via link público (sem auth). BusinessId vem do slug.</summary>
public record CreatePublicAppointmentRequest(
    Guid ServiceId,
    string ClientName,
    DateTime ScheduledAt,
    string? ClientPhone = null,
    string? ClientEmail = null,
    string? Notes = null);
