using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.DTOs.Auth;

/// <summary>Usuário autenticado (GET /api/auth/me).</summary>
public record CurrentUserResponse(Guid Id, string Email, string Name, string? ProfilePhotoUrl, DocumentType? DocumentType, string? DocumentNumber, bool IsAdmin, bool ShowWelcomeBanner, bool EmailVerified = true);
