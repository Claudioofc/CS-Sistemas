namespace CSSistemas.Domain.Entities;

/// <summary>Horário de funcionamento do negócio por dia da semana. DayOfWeek: 0=Domingo, 1=Segunda, ..., 6=Sábado.</summary>
public class BusinessHours : EntityBase
{
    public Guid BusinessId { get; protected set; }
    /// <summary>0=Domingo, 1=Segunda, ..., 6=Sábado (System.DayOfWeek).</summary>
    public int DayOfWeek { get; protected set; }
    /// <summary>Minutos desde meia-noite (0-1439). Ex.: 480 = 08:00.</summary>
    public int OpenAtMinutes { get; protected set; }
    /// <summary>Minutos desde meia-noite (0-1439). Ex.: 1080 = 18:00.</summary>
    public int CloseAtMinutes { get; protected set; }

    public Business Business { get; protected set; } = null!;

    protected BusinessHours() { }

    public static BusinessHours Create(Guid businessId, int dayOfWeek, int openAtMinutes, int closeAtMinutes)
    {
        if (businessId == Guid.Empty)
            throw new ArgumentException("BusinessId é obrigatório.", nameof(businessId));
        if (dayOfWeek < 0 || dayOfWeek > 6)
            throw new ArgumentException("DayOfWeek deve ser 0-6.", nameof(dayOfWeek));
        if (openAtMinutes < 0 || openAtMinutes > 1439)
            throw new ArgumentException("OpenAtMinutes deve ser 0-1439.", nameof(openAtMinutes));
        if (closeAtMinutes < 0 || closeAtMinutes > 1439)
            throw new ArgumentException("CloseAtMinutes deve ser 0-1439.", nameof(closeAtMinutes));
        if (openAtMinutes >= closeAtMinutes)
            throw new ArgumentException("OpenAt deve ser anterior a CloseAt.", nameof(closeAtMinutes));

        return new BusinessHours
        {
            BusinessId = businessId,
            DayOfWeek = dayOfWeek,
            OpenAtMinutes = openAtMinutes,
            CloseAtMinutes = closeAtMinutes
        };
    }

    public void Update(int openAtMinutes, int closeAtMinutes)
    {
        if (openAtMinutes < 0 || openAtMinutes > 1439)
            throw new ArgumentException("OpenAtMinutes deve ser 0-1439.", nameof(openAtMinutes));
        if (closeAtMinutes < 0 || closeAtMinutes > 1439)
            throw new ArgumentException("CloseAtMinutes deve ser 0-1439.", nameof(closeAtMinutes));
        if (openAtMinutes >= closeAtMinutes)
            throw new ArgumentException("OpenAt deve ser anterior a CloseAt.", nameof(closeAtMinutes));
        OpenAtMinutes = openAtMinutes;
        CloseAtMinutes = closeAtMinutes;
        UpdatedAt = DateTime.UtcNow;
    }
}
