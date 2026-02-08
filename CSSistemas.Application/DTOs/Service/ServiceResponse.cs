namespace CSSistemas.Application.DTOs.Service;

public record ServiceResponse(
    Guid Id,
    Guid BusinessId,
    string Name,
    int DurationMinutes,
    decimal? Price,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
