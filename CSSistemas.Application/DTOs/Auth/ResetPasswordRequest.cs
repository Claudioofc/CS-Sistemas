using System.Text.Json.Serialization;

namespace CSSistemas.Application.DTOs.Auth;

public record ResetPasswordRequest(
    [property: JsonPropertyName("token")] string Token,
    [property: JsonPropertyName("newPassword")] string NewPassword);
