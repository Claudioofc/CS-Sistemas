namespace CSSistemas.Application.DTOs.SystemMessage;

public record SystemMessageResponse(
    Guid Id,
    Guid BusinessId,
    string Key,
    string Title,
    string Body,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
