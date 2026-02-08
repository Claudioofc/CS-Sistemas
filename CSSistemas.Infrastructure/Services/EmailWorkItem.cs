namespace CSSistemas.Infrastructure.Services;

/// <summary>Item da fila de e-mail para processamento em background.</summary>
public sealed record EmailWorkItem(
    EmailWorkItemKind Kind,
    string? Email,
    string? ResetLink,
    string? ToEmail,
    string? ClientName,
    string? ScheduledAtFormatted,
    string? ServiceName,
    string? BusinessName,
    string? CancelLink,
    string? CancellationReason,
    string? NewUserRegisteredName = null,
    string? NewUserRegisteredEmail = null);

public enum EmailWorkItemKind
{
    PasswordReset,
    AppointmentConfirmation,
    AppointmentCancelledByProfessional,
    NewUserRegistered
}
