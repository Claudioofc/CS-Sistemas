namespace CSSistemas.Application.DTOs.PublicBooking;

public record PublicServiceDto(Guid Id, string Name, int DurationMinutes, decimal? Price);
