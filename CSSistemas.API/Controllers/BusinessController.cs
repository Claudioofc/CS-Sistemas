using CSSistemas.Application.DTOs;
using CSSistemas.Application.DTOs.Business;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using CSSistemas.API.Extensions;
using CSSistemas.Domain.Entities;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BusinessController : ControllerBase
{
    private readonly IBusinessRepository _repository;
    private readonly IBusinessHoursRepository _hoursRepository;
    private readonly IValidator<BusinessRequest> _validator;
    private readonly IValidator<BusinessHoursBulkRequest> _hoursValidator;
    private readonly IWebHostEnvironment _env;

    private const int MaxLogoBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedImageTypes = ["image/jpeg", "image/jpg", "image/png", "image/webp"];

    public BusinessController(
        IBusinessRepository repository,
        IBusinessHoursRepository hoursRepository,
        IValidator<BusinessRequest> validator,
        IValidator<BusinessHoursBulkRequest> hoursValidator,
        IWebHostEnvironment env)
    {
        _repository = repository;
        _hoursRepository = hoursRepository;
        _validator = validator;
        _hoursValidator = hoursValidator;
        _env = env;
    }

    /// <summary>Lista negócios do usuário autenticado.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BusinessResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var list = await _repository.GetByUserIdAsync(userId.Value, cancellationToken);
        var response = list.Select(ToResponse).ToList();
        return Ok(response);
    }

    /// <summary>Obtém negócio por id (apenas se pertencer ao usuário).</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _repository.GetByIdAndUserIdAsync(id, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        return Ok(ToResponse(business));
    }

    /// <summary>Cria negócio para o usuário autenticado.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] BusinessRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());

        var business = Business.Create(userId.Value, request.Name, request.BusinessType, request.PublicSlug);
        await _repository.AddAsync(business, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = business.Id }, ToResponse(business));
    }

    /// <summary>Atualiza negócio (apenas se pertencer ao usuário).</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(Guid id, [FromBody] BusinessRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());

        var business = await _repository.GetByIdAndUserIdForUpdateAsync(id, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        business.Update(request.Name, request.BusinessType, request.PublicSlug);
        await _repository.UpdateAsync(business, cancellationToken);
        return Ok(ToResponse(business));
    }

    /// <summary>Soft delete: marca negócio como excluído (não remove do banco).</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var deleted = await _repository.SoftDeleteAsync(id, userId.Value, cancellationToken);
        if (!deleted) throw CommException.NotFound("Negócio não encontrado.");
        return NoContent();
    }

    /// <summary>Lista horários de funcionamento do negócio (0=Domingo a 6=Sábado). Dia sem registro = fechado.</summary>
    [HttpGet("{id:guid}/hours")]
    [ProducesResponseType(typeof(IReadOnlyList<BusinessHoursItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHours(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _repository.GetByIdAndUserIdAsync(id, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var hours = await _hoursRepository.GetByBusinessIdAsync(id, cancellationToken);
        var byDay = hours.ToDictionary(h => h.DayOfWeek);
        var response = Enumerable.Range(0, 7).Select(day => new BusinessHoursItemResponse(
            day,
            byDay.TryGetValue(day, out var h) ? h.OpenAtMinutes : null,
            byDay.TryGetValue(day, out h) ? h.CloseAtMinutes : null)).ToList();
        return Ok(response);
    }

    /// <summary>Atualiza horários de funcionamento (lista de 7 dias: 0 a 6). Open/Close null = dia fechado.</summary>
    [HttpPut("{id:guid}/hours")]
    [ProducesResponseType(typeof(IReadOnlyList<BusinessHoursItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateHours(Guid id, [FromBody] BusinessHoursBulkRequest request, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var validation = await _hoursValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid) return BadRequest(validation.ToValidationErrorResponse());
        var business = await _repository.GetByIdAndUserIdAsync(id, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        var existing = await _hoursRepository.GetByBusinessIdForUpdateAsync(id, cancellationToken);
        var byDay = existing.ToDictionary(h => h.DayOfWeek);
        foreach (var item in request.Items!)
        {
            if (item.OpenAtMinutes == null || item.CloseAtMinutes == null)
            {
                if (byDay.TryGetValue(item.DayOfWeek, out var toDelete))
                    toDelete.MarkAsDeleted();
                continue;
            }
            if (byDay.TryGetValue(item.DayOfWeek, out var toUpdate))
            {
                toUpdate.Update(item.OpenAtMinutes.Value, item.CloseAtMinutes.Value);
            }
            else
            {
                var newHour = BusinessHours.Create(id, item.DayOfWeek, item.OpenAtMinutes.Value, item.CloseAtMinutes.Value);
                await _hoursRepository.AddAsync(newHour, cancellationToken);
            }
        }
        foreach (var e in existing)
            await _hoursRepository.UpdateAsync(e, cancellationToken);
        var updated = await _hoursRepository.GetByBusinessIdAsync(id, cancellationToken);
        var byDayUpdated = updated.ToDictionary(h => h.DayOfWeek);
        var response = Enumerable.Range(0, 7).Select(day => new BusinessHoursItemResponse(
            day,
            byDayUpdated.TryGetValue(day, out var h) ? h.OpenAtMinutes : null,
            byDayUpdated.TryGetValue(day, out h) ? h.CloseAtMinutes : null)).ToList();
        return Ok(response);
    }

    /// <summary>Faz upload da logo do negócio.</summary>
    [HttpPost("{id:guid}/logo")]
    [ProducesResponseType(typeof(BusinessResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadLogo(Guid id, IFormFile? file, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var business = await _repository.GetByIdAndUserIdForUpdateAsync(id, userId.Value, cancellationToken);
        if (business == null) throw CommException.NotFound("Negócio não encontrado.");
        if (file == null || file.Length == 0)
            throw CommException.BadRequest("Selecione uma imagem (JPEG, PNG ou WebP, até 5 MB).");
        if (file.Length > MaxLogoBytes)
            throw CommException.BadRequest("A imagem deve ter no máximo 5 MB.");
        var contentType = file.ContentType?.ToLowerInvariant() ?? "";
        if (!AllowedImageTypes.Contains(contentType))
            throw CommException.BadRequest("Formato não permitido. Use JPEG, PNG ou WebP.");
        var ext = contentType switch
        {
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => ".jpg"
        };
        var webRoot = _env.WebRootPath ?? Path.Combine(_env.ContentRootPath, "wwwroot");
        var uploadDir = Path.Combine(webRoot, "uploads", "logos");
        Directory.CreateDirectory(uploadDir);
        var fileName = $"{id:N}{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(uploadDir, fileName);
        await using (var stream = new FileStream(fullPath, FileMode.Create))
            await file.CopyToAsync(stream, cancellationToken);
        var url = $"/uploads/logos/{fileName}";
        business.UpdateLogo(url);
        await _repository.UpdateAsync(business, cancellationToken);
        return Ok(ToResponse(business));
    }

    private static BusinessResponse ToResponse(Business b) => new(
        b.Id, b.UserId, b.Name, b.BusinessType, b.PublicSlug, b.LogoUrl, b.CreatedAt, b.UpdatedAt);

}
