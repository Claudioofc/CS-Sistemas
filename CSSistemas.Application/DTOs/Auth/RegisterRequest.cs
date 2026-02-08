namespace CSSistemas.Application.DTOs.Auth;

public record RegisterRequest(string Email, string Password, string Name, int DocumentType, string DocumentNumber);
