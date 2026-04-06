using CSSistemas.API.Extensions;
using CSSistemas.Application.Configuration;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IO;

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
    [Consumes("multipart/form-data")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Contact([FromForm] SupportContactRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();

        var messageTrimmed = request.Message?.Trim();
        if (string.IsNullOrWhiteSpace(messageTrimmed))
            return BadRequest(new { message = "A mensagem é obrigatória." });

        const int maxAttachmentSizeBytes = 10 * 1024 * 1024; // 10 MB
        if (request.Attachment != null && request.Attachment.Length > maxAttachmentSizeBytes)
            return BadRequest(new { message = "O anexo deve ter no máximo 10 MB." });

        if (request.Attachment != null && request.Attachment.Length > 0)
        {
            if (!SupportAttachmentValidator.IsAllowedContentType(request.Attachment))
                return BadRequest(new { message = "Tipo de arquivo não permitido. Envie imagem (JPEG, PNG, WebP, GIF) ou PDF." });
            if (!SupportAttachmentValidator.HasValidMagicBytes(request.Attachment))
                return BadRequest(new { message = "Arquivo inválido. O conteúdo não corresponde ao tipo declarado." });
        }

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
        if (request.Attachment != null && request.Attachment.Length > 0)
        {
            using var memoryStream = new MemoryStream();
            await request.Attachment.CopyToAsync(memoryStream, cancellationToken);
            attachmentBytes = memoryStream.ToArray();
            attachmentFileName = request.Attachment.FileName;
        }

        await _emailSender.SendSupportRequestAsync(
            adminEmail,
            user.Name,
            user.Email,
            messageTrimmed,
            string.IsNullOrWhiteSpace(request.PageUrl) ? null : request.PageUrl.Trim(),
            attachmentBytes,
            attachmentFileName,
            cancellationToken);

        return Ok(new { message = "Mensagem enviada. Entraremos em contato em breve." });
    }
}

public sealed class SupportContactRequest
{
    public string Message { get; set; } = string.Empty;
    public string? PageUrl { get; set; }
    public IFormFile? Attachment { get; set; }
}

/// <summary>Validação de tipos de arquivo permitidos no suporte (imagens e PDF).</summary>
internal static class SupportAttachmentValidator
{
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp", "image/gif",
        "application/pdf"
    };

    public static bool IsAllowedContentType(IFormFile file)
        => AllowedContentTypes.Contains(file.ContentType?.ToLowerInvariant() ?? "");

    public static bool HasValidMagicBytes(IFormFile file)
    {
        var header = new byte[12];
        using var stream = file.OpenReadStream();
        var read = stream.Read(header, 0, header.Length);
        if (read < 4) return false;
        // JPEG: FF D8 FF
        if (header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF) return true;
        // PNG: 89 50 4E 47
        if (header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47) return true;
        // GIF: 47 49 46 38
        if (header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38) return true;
        // WebP: RIFF????WEBP
        if (read >= 12 && header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
            && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50) return true;
        // PDF: 25 50 44 46 (%PDF)
        if (header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46) return true;
        return false;
    }
}
