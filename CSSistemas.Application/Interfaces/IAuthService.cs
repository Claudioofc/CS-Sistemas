using CSSistemas.Application.DTOs.Auth;

namespace CSSistemas.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    /// <summary>Esqueci minha senha: gera token e envia e-mail com link (não revela se o e-mail existe).</summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default);
    /// <summary>Redefine a senha com o token recebido por e-mail. Retorna true se válido.</summary>
    Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
}
