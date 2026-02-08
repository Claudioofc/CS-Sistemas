using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.DTOs.Business;

public record BusinessResponse(
    Guid Id,
    Guid UserId,
    string Name,
    BusinessType BusinessType,
    string? PublicSlug,
    string? WhatsAppPhone,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
