namespace CSSistemas.Application.DTOs.Auth;

public record LoginResponse(string Token, string Email, string Name, DateTime ExpiresAt, string? ProfilePhotoUrl = null);
