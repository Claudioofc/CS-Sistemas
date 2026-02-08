using CSSistemas.Domain.Enums;

namespace CSSistemas.Domain.Entities;

/// <summary>
/// Assinatura do usuário (trial ou plano pago). Define se o usuário tem acesso ao sistema.
/// Trial: 7 dias automáticos ao criar conta. Depois, exige plano pago.
/// </summary>
public class Subscription : EntityBase
{
    public Guid UserId { get; protected set; }
    public User User { get; protected set; } = null!;
    public SubscriptionType SubscriptionType { get; protected set; }
    public DateTime StartedAt { get; protected set; }
    public DateTime EndsAt { get; protected set; }

    protected Subscription() { }

    /// <summary>Cria assinatura trial de 7 dias.</summary>
    public static Subscription CreateTrial(Guid userId)
    {
        var now = DateTime.UtcNow;
        return new Subscription
        {
            UserId = userId,
            SubscriptionType = SubscriptionType.Trial,
            StartedAt = now,
            EndsAt = now.AddDays(7)
        };
    }

    /// <summary>Cria assinatura paga (Premium) a partir do plano: Monthly com duração conforme billingIntervalMonths.</summary>
    public static Subscription CreateFromPlan(Guid userId, int billingIntervalMonths)
    {
        var now = DateTime.UtcNow;
        var endsAt = billingIntervalMonths <= 0 ? now.AddMonths(1) : now.AddMonths(billingIntervalMonths);
        return new Subscription
        {
            UserId = userId,
            SubscriptionType = SubscriptionType.Monthly,
            StartedAt = now,
            EndsAt = endsAt
        };
    }

    /// <summary>Indica se a assinatura ainda está válida (dentro do período).</summary>
    public bool IsActive => DateTime.UtcNow <= EndsAt;
}
