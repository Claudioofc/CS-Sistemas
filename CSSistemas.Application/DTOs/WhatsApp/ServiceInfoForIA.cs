namespace CSSistemas.Application.DTOs.WhatsApp;

/// <summary>Dados de um serviço passados para a IA (nome, duração, preço). A IA responde APENAS com base nesses dados; nunca inventa valores.</summary>
public record ServiceInfoForIA(string Name, int DurationMinutes, decimal? Price);
