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
    public async Task<IActionResult> Contact([FromForm] string message, [FromForm] string? pageUrl, [FromForm] IFormFile? attachment, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var messageTrimmed = message?.Trim();
        if (string.IsNullOrWhiteSpace(messageTrimmed))
            return BadRequest(new { message = "A mensagem é obrigatória." });

        const int maxAttachmentSizeBytes = 10 * 1024 * 1024; // 10 MB
        if (attachment != null && attachment.Length > maxAttachmentSizeBytes)
            return BadRequest(new { message = "O anexo deve ter no máximo 10 MB." });

        var adminEmail = _adminSettings.NotificationEmail?.Trim();
        if (string.IsNullOrEmpty(adminEmail))
        {
            _logger.LogWarning("Fale conosco chamado mas Admin:NotificationEmail não configurado.");
            return BadRequest(new { message = "O contato de suporte não está configurado no momento. Tente novamente mais tarde ou entre em contato por outro canal." });
        }

        var user = await _userRepository.GetByIdAsync(userId.Value, cancellationToken);
        if (user == null) return Unauthorized();

        byte[]? attachmentBytes = null;
        string? attachmentFileName = null;
        if (attachment != null && attachment.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            await attachment.CopyToAsync(memoryStream, cancellationToken);
            attachmentBytes = memoryStream.ToArray();
            attachmentFileName = attachment.FileName;
        }

        await _emailSender.SendSupportRequestAsync(
            adminEmail,
            user.Name,
            user.Email,
            messageTrimmed,
            string.IsNullOrWhiteSpace(pageUrl) ? null : pageUrl.Trim(),
            attachmentBytes,
            attachmentFileName,
            cancellationToken);

        return Ok(new { message = "Mensagem enviada. Entraremos em contato em breve." });
    }
}
