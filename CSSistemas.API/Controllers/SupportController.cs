using CSSistemas.API.Extensions;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.DTOs.Support;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SupportController : ControllerBase
{
    private readonly IEmailSender _emailSender;
    private readonly IUserRepository _userRepository;
    private readonly AdminSettings _adminSettings;
    private readonly ILogger<SupportController> _logger;

    public SupportController(
        IEmailSender emailSender,
        IUserRepository userRepository,
        IOptions<AdminSettings> adminSettings,
        ILogger<SupportController> logger)
    {
        _emailSender = emailSender;
        _userRepository = userRepository;
        _adminSettings = adminSettings.Value;
        _logger = logger;
    }

    /// <summary>Envia mensagem de suporte (Fale conosco / Reportar problema) para o administrador.</summary>
    [HttpPost("contact")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Contact([FromBody] ContactRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var message = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(message))
            return BadRequest(new { message = "A mensagem é obrigatória." });

        var adminEmail = _adminSettings.NotificationEmail?.Trim();
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Fale conosco chamado mas Admin:NotificationEmail não configurado.");
            return BadRequest(new { message = "O contato de suporte não está configurado no momento. Tente novamente mais tarde ou entre em contato por outro canal." });
        }

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) return Unauthorized();

        await _emailSender.SendSupportRequestAsync(
            adminEmail,
            user.Name,
            user.Email,
            message,
            string.IsNullOrWhiteSpace(request.PageUrl) ? null : request.PageUrl.Trim(),
            cancellationToken);

        return Ok(new { message = "Mensagem enviada. Entraremos em contato em breve." });
    }
}
