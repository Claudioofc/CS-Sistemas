namespace CSSistemas.Application.DTOs.Subscription;

public record SubscriptionRequest(Guid UserId, Guid PlanId, DateTime StartDate, DateTime EndDate, string? ExternalId = null);
