namespace CSSistemas.Application.DTOs.PublicBooking;

/// <summary>Dados públicos do negócio para página de agendamento (sem auth).</summary>
public record PublicBusinessDto(Guid Id, string Name, string? PublicSlug);
