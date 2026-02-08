using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.DTOs.Auth;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using Microsoft.Extensions.Options;
using CSSistemas.Domain.Entities;
using CSSistemas.Domain.Enums;
using Microsoft.IdentityModel.Tokens;

namespace CSSistemas.Infrastructure.Services;

public class AuthService : IAuthService
{
    private const int ResetTokenExpiryHours = 1;

    private readonly IUserRepository _userRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly INotificationRepository _notificationRepository;
    private readonly IEmailSender _emailSender;
    private readonly JwtSettings _jwtSettings;
    private readonly EmailSettings _emailSettings;
    private readonly AdminSettings _adminSettings;

    public AuthService(
        IUserRepository userRepository,
        ISubscriptionRepository subscriptionRepository,
        INotificationRepository notificationRepository,
        IEmailSender emailSender,
        IOptions<JwtSettings> jwtSettings,
        IOptions<EmailSettings> emailSettings,
        IOptions<AdminSettings> adminSettings)
    {
        _userRepository = userRepository;
        _subscriptionRepository = subscriptionRepository;
        _notificationRepository = notificationRepository;
        _emailSender = emailSender;
        _jwtSettings = jwtSettings.Value;
        _emailSettings = emailSettings.Value;
        _adminSettings = adminSettings.Value;
    }

    private const int MaxLoginAttempts = 3;

    public async Task<LoginResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return new LoginFailureResult(MaxLoginAttempts);

        var user = await _userRepository.GetByEmailForUpdateAsync(request.Email, cancellationToken);
        if (user == null)
            return new LoginFailureResult(MaxLoginAttempts);

        // Bloqueio indefinido após 3 falhas; só desbloqueia ao redefinir senha
        if (user.LockoutEnd.HasValue)
            return new LoginLockedResult(user.LockoutEnd.Value);

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin();
            await _userRepository.UpdateAsync(user, cancellationToken);
            var remaining = Math.Max(0, MaxLoginAttempts - user.FailedLoginAttempts);
            return new LoginFailureResult(remaining);
        }

        user.ResetFailedLogins();
        await _userRepository.UpdateAsync(user, cancellationToken);
        var jwt = GenerateToken(user.Id, user.Email, user.Name);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes);
        return new LoginSuccessResult(new LoginResponse(jwt, user.Email, user.Name, expiresAt, user.ProfilePhotoUrl));
    }

    public async Task<LoginResponse?> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password) || string.IsNullOrWhiteSpace(request.Name))
            return null;

        var existing = await _userRepository.GetByEmailAsync(request.Email, cancellationToken);
        if (existing != null)
            return null;

        var docType = request.DocumentType == 1 ? DocumentType.Cnpj : DocumentType.Cpf;
        var docDigits = new string((request.DocumentNumber ?? "").Where(char.IsDigit).ToArray());
        if (string.IsNullOrEmpty(docDigits))
            throw CommException.BadRequest("CPF ou CNPJ é obrigatório.");
        var docAlreadyUsed = await _userRepository.ExistsByDocumentAsync(docType, docDigits, null, cancellationToken);
        if (docAlreadyUsed)
            throw CommException.Conflict("Este CPF/CNPJ já possui uma conta. Faça login na sua conta ou assine um plano para continuar.");

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
        var user = User.Create(request.Email, passwordHash, request.Name, docType, docDigits);
        await _userRepository.AddAsync(user, cancellationToken);

        var trial = Subscription.CreateTrial(user.Id);
        await _subscriptionRepository.AddAsync(trial, cancellationToken);

        var adminIds = await _userRepository.GetAdminIdsAsync(cancellationToken);
        foreach (var adminId in adminIds)
        {
            var notification = Notification.CreateNewUserRegistered(adminId, user.Name, user.Email);
            await _notificationRepository.AddAsync(notification, cancellationToken);
        }

        var adminEmail = _adminSettings.NotificationEmail?.Trim();
        if (!string.IsNullOrEmpty(adminEmail))
            await _emailSender.SendNewUserRegisteredAsync(adminEmail, user.Name, user.Email, cancellationToken);

        var token = GenerateToken(user.Id, user.Email, user.Name);
        var expiresAt = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes);
        return new LoginResponse(token, user.Email, user.Name, expiresAt, user.ProfilePhotoUrl);
    }

    private string GenerateToken(Guid userId, string email, string name)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, email),
            new Claim(JwtRegisteredClaimNames.GivenName, name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpiresMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var email = request.Email?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(email)) return;

        var user = await _userRepository.GetByEmailForUpdateAsync(email, cancellationToken);
        if (user == null) return; // Não revela se o e-mail existe

        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddHours(ResetTokenExpiryHours);
        user.SetPasswordResetToken(token, expiresAt);
        await _userRepository.UpdateAsync(user, cancellationToken);

        var baseUrl = (_emailSettings.PasswordResetBaseUrl ?? "").TrimEnd('/');
        var resetLink = string.IsNullOrEmpty(baseUrl) ? token : $"{baseUrl}/redefinir-senha?token={token}";
        await _emailSender.SendPasswordResetAsync(email, resetLink, cancellationToken);
    }

    public async Task<bool> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Token) || string.IsNullOrWhiteSpace(request.NewPassword))
            return false;

        var user = await _userRepository.GetByResetTokenForUpdateAsync(request.Token.Trim(), cancellationToken);
        if (user == null) return false;

        var hash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.SetPassword(hash);
        user.ClearPasswordReset();
        user.ResetFailedLogins(); // Desbloqueia a conta após redefinir senha
        await _userRepository.UpdateAsync(user, cancellationToken);
        return true;
    }
}
