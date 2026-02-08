namespace CSSistemas.Application.DTOs.Appointment;

public record AppointmentRequest(
    Guid BusinessId,
    Guid ServiceId,
    string ClientName,
    DateTime ScheduledAt,
    string? ClientPhone = null,
    string? ClientEmail = null,
    string? Notes = null);
