namespace CSSistemas.Application.DTOs.Service;

public record ServiceRequest(Guid BusinessId, string Name, int DurationMinutes, decimal? Price);
