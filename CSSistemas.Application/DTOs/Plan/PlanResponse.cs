namespace CSSistemas.Application.DTOs.Plan;

public record PlanResponse(Guid Id, string Name, decimal Price, int BillingIntervalMonths, string? Features, bool IsActive, DateTime CreatedAt);
