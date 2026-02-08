namespace CSSistemas.Application.DTOs.Support;

/// <summary>Corpo do POST Fale conosco / Reportar problema.</summary>
public record ContactRequest(string Message, string? PageUrl = null);
