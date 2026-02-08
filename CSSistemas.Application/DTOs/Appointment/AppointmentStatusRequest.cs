using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.DTOs.Appointment;

public record AppointmentStatusRequest(AppointmentStatus Status, string? CancellationReason = null);
