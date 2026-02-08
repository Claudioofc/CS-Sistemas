using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.DTOs.Appointment;

public record AppointmentResponse(
    Guid Id,
    Guid BusinessId,
    Guid ServiceId,
    string ClientName,
    string? ClientPhone,
    string? ClientEmail,
    DateTime ScheduledAt,
    AppointmentStatus Status,
    string? Notes,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
