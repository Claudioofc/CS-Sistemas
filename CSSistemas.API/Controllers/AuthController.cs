using CSSistemas.API.Extensions;
using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.Auth;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const int MaxProfilePhotoBytes = 5 * 1024 * 1024; // 5 MB
    private static readonly string[] AllowedImageContentTypes = { "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif" };

    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepository;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<ForgotPasswordRequest> _forgotPasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator;
    private readonly IValidator<ProfileUpdateRequest> _profileValidator;
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAuthService authService,
        IUserRepository userRepository,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<ForgotPasswordRequest> forgotPasswordValidator,
        IValidator<ResetPasswordRequest> resetPasswordValidator,
        IValidator<ProfileUpdateRequest> profileValidator,
        IWebHostEnvironment env,
        ILogger<AuthController> logger)
    {
        _authService = authService;
        _userRepository = userRepository;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _forgotPasswordValidator = forgotPasswordValidator;
        _resetPasswordValidator = resetPasswordValidator;
        _profileValidator = profileValidator;
        _env = env;
        _logger = logger;
    }

    /// <summary>Login — retorna JWT para web e mobile.</summary>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var validation = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToValidationErrorResponse());

        var result = await _authService.LoginAsync(request, cancellationToken);
        return result switch
        {
            LoginSuccessResult success => Ok(success.Response),
            LoginFailureResult fail => Unauthorized(new { message = "Email ou senha inválidos.", attemptsRemaining = fail.AttemptsRemaining }),
            LoginLockedResult _ => Unauthorized(new { message = "Conta bloqueada. Use 'Esqueci minha senha' para redefinir sua senha por e-mail.", locked = true }),
            _ => Unauthorized(new { message = "Email ou senha inválidos.", attemptsRemaining = 3 })
        };
    }

    /// <summary>Registro — cria usuário e retorna JWT.</summary>
    [HttpPost("register")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
    {
        var validation = await _registerValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToValidationErrorResponse());

        try
        {
            var result = await _authService.RegisterAsync(request, cancellationToken);
            if (result == null)
                throw CommException.BadRequest("Email já cadastrado.");
            return Ok(result);
        }
        catch (DbUpdateException)
        {
            throw CommException.BadRequest("Email já cadastrado.");
        }
    }

    /// <summary>Esqueci minha senha: envia e-mail com link para redefinir (não revela se o e-mail existe).</summary>
    [HttpPost("forgot-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
    {
        var validation = await _forgotPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToValidationErrorResponse());

        await _authService.ForgotPasswordAsync(request, cancellationToken);
        return Ok(new
        {
            message = "Se o e-mail estiver cadastrado, você receberá em instantes um link para redefinir sua senha. Verifique sua caixa de entrada e a pasta de spam. O link é válido por 1 hora."
        });
    }

    /// <summary>Redefine a senha com o token recebido por e-mail.</summary>
    [HttpPost("reset-password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest? request, CancellationToken cancellationToken)
    {
        _logger.LogInformation("ResetPassword: request is null={RequestNull}", request == null);
        if (request == null)
        {
            _logger.LogWarning("ResetPassword: corpo da requisição nulo ou inválido.");
            return BadRequest(new ValidationErrorResponse("Corpo da requisição inválido. Envie token e newPassword.", Array.Empty<CampoErro>()));
        }
        _logger.LogInformation("ResetPassword: Token length={TokenLen}, NewPassword length={NewPasswordLen}",
            request.Token?.Length ?? 0, request.NewPassword?.Length ?? 0);
        var validation = await _resetPasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            var errMsgs = string.Join("; ", validation.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            _logger.LogWarning("ResetPassword: validação falhou. Erros: {Erros}", errMsgs);
            return BadRequest(validation.ToValidationErrorResponse());
        }
        var success = await _authService.ResetPasswordAsync(request, cancellationToken);
        if (!success)
        {
            _logger.LogWarning("ResetPassword: token inválido ou expirado.");
            throw CommException.BadRequest("Token inválido ou expirado. Solicite uma nova redefinição de senha.");
        }
        _logger.LogInformation("ResetPassword: senha redefinida com sucesso.");
        return Ok(new { message = "Senha redefinida com sucesso." });
    }

    /// <summary>Retorna o usuário autenticado (perfil com foto).</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMe(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) throw CommException.NotFound("Usuário não encontrado.");
        return Ok(new CurrentUserResponse(user.Id, user.Email, user.Name, user.ProfilePhotoUrl, user.DocumentType, user.DocumentNumber, user.IsAdmin));
    }

    /// <summary>Envia uma imagem como foto de perfil. Retorna a URL relativa para salvar no perfil.</summary>
    [HttpPost("profile-photo")]
    [Authorize]
    [ProducesResponseType(typeof(ProfilePhotoUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> UploadProfilePhoto(IFormFile? file, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        if (file == null || file.Length == 0)
            throw CommException.BadRequest("Selecione uma imagem (JPEG, PNG, WebP ou GIF, até 5 MB).");
        if (file.Length > MaxProfilePhotoBytes)
            throw CommException.BadRequest("A imagem deve ter no máximo 5 MB.");
        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!AllowedImageContentTypes.Contains(contentType))
            throw CommException.BadRequest("Formato não permitido. Use JPEG, PNG, WebP ou GIF.");
        var ext = contentType switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            "image/gif" => ".gif",
            _ => ".jpg"
        };
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadDir = Path.Combine(webRoot, "uploads", "profile");
        Directory.CreateDirectory(uploadDir);
        var fileName = $"{userId:N}{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);
        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream, cancellationToken);
        var url = $"/uploads/profile/{fileName}";
        return Ok(new ProfilePhotoUploadResponse(url));
    }

    /// <summary>Atualiza nome, foto e/ou documento (CPF/CNPJ).</summary>
    [HttpPatch("profile")]
    [Authorize]
    [ProducesResponseType(typeof(CurrentUserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProfile([FromBody] ProfileUpdateRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _profileValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.ToValidationErrorResponse());
        var user = await _userRepository.GetByIdForUpdateAsync(userId.Value, cancellationToken);
        if (user == null) throw CommException.NotFound("Usuário não encontrado.");
        // Salva documento apenas com dígitos (aceita envio com ou sem formatação)
        var documentNumberOnly = string.IsNullOrWhiteSpace(request.DocumentNumber)
            ? null
            : new string(request.DocumentNumber.Where(char.IsDigit).ToArray());
        if (request.DocumentType.HasValue && !string.IsNullOrEmpty(documentNumberOnly))
        {
            var docType = request.DocumentType.Value;
            var alreadyUsed = await _userRepository.ExistsByDocumentAsync(docType, documentNumberOnly, userId, cancellationToken);
            if (alreadyUsed)
                throw CommException.Conflict("Este CPF/CNPJ já está vinculado a outra conta. Use a conta original ou assine um plano.");
        }
        user.UpdateProfile(request.Name, request.ProfilePhotoUrl, request.DocumentType, documentNumberOnly);
        await _userRepository.UpdateAsync(user, cancellationToken);
        return Ok(new CurrentUserResponse(user.Id, user.Email, user.Name, user.ProfilePhotoUrl, user.DocumentType, user.DocumentNumber, user.IsAdmin));
    }

}
