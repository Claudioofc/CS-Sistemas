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
    string? NewUserRegisteredEmail = null,
    string? SupportRequestUserName = null,
    string? SupportRequestUserEmail = null,
    string? SupportRequestMessage = null,
    string? SupportRequestPageUrl = null,
    byte[]? SupportRequestAttachment = null,
    string? SupportRequestAttachmentFileName = null,
    string? WelcomeUserName = null,
    string? ExpiryWarningPlanName = null,
    string? ExpiryWarningEndsAt = null,
    int? ExpiryWarningDays = null,
    string? TwoFactorUserName = null,
    string? TwoFactorCode = null);

public enum EmailWorkItemKind
{
    PasswordReset,
    AppointmentConfirmation,
    AppointmentCancelledByProfessional,
    AppointmentCancelledByClient,
    NewUserRegistered,
    NewUserWelcome,
    SupportRequest,
    SubscriptionExpiryWarning,
    AppointmentReminder,
    EmailVerification
}
