using CSSistemas.Domain.Enums;

namespace CSSistemas.Application.DTOs.Auth;

/// <summary>Atualização de perfil (nome, foto e documento CPF/CNPJ obrigatório).</summary>
public record ProfileUpdateRequest(string Name, string? ProfilePhotoUrl, DocumentType? DocumentType, string? DocumentNumber);
