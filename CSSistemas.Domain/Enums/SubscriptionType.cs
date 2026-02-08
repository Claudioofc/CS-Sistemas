namespace CSSistemas.Domain.Enums;

/// <summary>Tipo de assinatura: trial (7 dias grátis) ou plano pago.</summary>
public enum SubscriptionType
{
    /// <summary>Período de teste de 7 dias.</summary>
    Trial = 0,

    /// <summary>Plano pago mensal (futuro).</summary>
    Monthly = 1
}
