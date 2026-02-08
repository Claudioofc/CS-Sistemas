namespace CSSistemas.Application.DTOs.Client;

public record ClientRequest(
    Guid BusinessId,
    string Name,
    string? Phone = null,
    string? Email = null,
    string? Notes = null);
