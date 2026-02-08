namespace CSSistemas.Domain.Entities;

/// <summary>Notificação para o usuário (ex.: novo agendamento pelo link público).</summary>
public class Notification : EntityBase
{
    public Guid UserId { get; protected set; }
    /// <summary>Tipo da notificação (ex.: NewAppointment).</summary>
    public string Type { get; protected set; } = string.Empty;
    public string ClientName { get; protected set; } = string.Empty;
    public DateTime ScheduledAt { get; protected set; }
    public Guid? AppointmentId { get; protected set; }
    public DateTime? ReadAt { get; protected set; }

    public User User { get; protected set; } = null!;

    protected Notification() { }

    public static Notification CreateNewAppointment(Guid userId, string clientName, DateTime scheduledAt, Guid appointmentId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId é obrigatório.", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("ClientName é obrigatório.", nameof(clientName));
        return new Notification
        {
            UserId = userId,
            Type = "NewAppointment",
            ClientName = clientName.Trim(),
            ScheduledAt = scheduledAt,
            AppointmentId = appointmentId
        };
    }

    /// <summary>Notificação quando o cliente cancela pelo link do e-mail.</summary>
    public static Notification CreateAppointmentCancelledByClient(Guid userId, string clientName, DateTime scheduledAt, Guid appointmentId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId é obrigatório.", nameof(userId));
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("ClientName é obrigatório.", nameof(clientName));
        return new Notification
        {
            UserId = userId,
            Type = "AppointmentCancelledByClient",
            ClientName = clientName.Trim(),
            ScheduledAt = scheduledAt,
            AppointmentId = appointmentId
        };
    }

    /// <summary>Notificação para admin: novo usuário se cadastrou. clientName exibido = "Nome (email)".</summary>
    public static Notification CreateNewUserRegistered(Guid adminUserId, string newUserName, string newUserEmail)
    {
        if (adminUserId == Guid.Empty)
            throw new ArgumentException("AdminUserId é obrigatório.", nameof(adminUserId));
        var display = string.IsNullOrWhiteSpace(newUserEmail)
            ? (newUserName?.Trim() ?? "—")
            : $"{newUserName?.Trim() ?? "—"} ({newUserEmail.Trim()})";
        return new Notification
        {
            UserId = adminUserId,
            Type = "NewUserRegistered",
            ClientName = display,
            ScheduledAt = DateTime.UtcNow,
            AppointmentId = null
        };
    }

    public void MarkAsRead()
    {
        if (ReadAt.HasValue) return;
        ReadAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
