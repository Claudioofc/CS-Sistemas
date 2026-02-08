namespace CSSistemas.Application.DTOs.Subscription;

/// <summary>Status da assinatura do usuário (para exibir "X dias restantes" ou redirecionar quando expirado).</summary>
public record SubscriptionStatusResponse(
    bool HasAccess,
    DateTime? EndsAt,
    bool IsTrial,
    int? DaysRemaining
);
