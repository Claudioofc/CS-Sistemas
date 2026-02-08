using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.DTOs.Business;

public record BusinessRequest(string Name, BusinessType BusinessType, string? PublicSlug, string? WhatsAppPhone = null);
