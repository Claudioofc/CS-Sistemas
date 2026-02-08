namespace CSSistemas.Application.DTOs.SystemMessage;

public record SystemMessageRequest(
    Guid BusinessId,
    string Key,
    string Title,
    string Body);
