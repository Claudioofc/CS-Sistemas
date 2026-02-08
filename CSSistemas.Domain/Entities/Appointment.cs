using CSSistemas.Domain.Enums;

namespace CSSistemas.Domain.Entities;

/// <summary>Agendamento de um cliente final para um serviço.</summary>
public class Appointment : EntityBase
{
    public Guid BusinessId { get; protected set; }
    public Guid ServiceId { get; protected set; }
    public string ClientName { get; protected set; } = string.Empty;
    public string? ClientPhone { get; protected set; }
    public string? ClientEmail { get; protected set; }
    public DateTime ScheduledAt { get; protected set; }
    public AppointmentStatus Status { get; protected set; } = AppointmentStatus.Pending;
    public string? Notes { get; protected set; }
    /// <summary>Token para o cliente cancelar pelo link do e-mail (agendamento público).</summary>
    public string? CancelToken { get; protected set; }

    public Business Business { get; protected set; } = null!;
    public Service Service { get; protected set; } = null!;

    protected Appointment() { }

    public static Appointment Create(
        Guid businessId,
        Guid serviceId,
        string clientName,
        DateTime scheduledAt,
        string? clientPhone = null,
        string? clientEmail = null,
        string? notes = null)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId é obrigatório.", nameof(businessId));
        if (serviceId == Guid.Empty)
            throw new ArgumentException("ServiceId é obrigatório.", nameof(serviceId));
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentException("Nome do cliente é obrigatório.", nameof(clientName));
        if (scheduledAt.Kind != DateTimeKind.Utc)
            scheduledAt = DateTime.SpecifyKind(scheduledAt, DateTimeKind.Utc);

        return new Appointment
        {
            BusinessId = businessId,
            ServiceId = serviceId,
            ClientName = clientName.Trim(),
            ClientPhone = string.IsNullOrWhiteSpace(clientPhone) ? null : clientPhone.Trim(),
            ClientEmail = string.IsNullOrWhiteSpace(clientEmail) ? null : clientEmail.Trim().ToLowerInvariant(),
            ScheduledAt = scheduledAt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim()
        };
    }

    public void SetStatus(AppointmentStatus status)
    {
        Status = status;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetCancelToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token é obrigatório.", nameof(token));
        CancelToken = token.Trim();
        UpdatedAt = DateTime.UtcNow;
    }
}
