namespace CSSistemas.Application.DTOs.Client;

public record ClientResponse(
    Guid Id,
    Guid BusinessId,
    string Name,
    string? Phone,
    string? Email,
    string? Notes,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
