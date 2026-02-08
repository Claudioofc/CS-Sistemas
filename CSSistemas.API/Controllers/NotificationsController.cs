using CSSistemas.API.Extensions;
using CSSistemas.Application.Exceptions;
using CSSistemas.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CSSistemas.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationsController(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    /// <summary>Lista notificações do usuário (ex.: novo agendamento — nome, data, horário).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(NotificationItemDto[]), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var list = await _notificationRepository.GetByUserIdAsync(userId.Value, 50, cancellationToken);
        var dtos = list.Select(n => new NotificationItemDto(
            n.Id,
            n.Type,
            n.ClientName,
            n.ScheduledAt,
            n.ReadAt,
            n.CreatedAt
        )).ToList();
        return Ok(dtos);
    }

    /// <summary>Marca notificação como lida (fechar).</summary>
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MarkAsRead(Guid id, CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();
        if (userId == null) return Unauthorized();
        var notification = await _notificationRepository.GetByIdAndUserIdAsync(id, userId.Value, cancellationToken);
        if (notification == null) throw CommException.NotFound("Notificação não encontrada.");
        notification.MarkAsRead();
        await _notificationRepository.UpdateAsync(notification, cancellationToken);
        return NoContent();
    }
}

public record NotificationItemDto(
    Guid Id,
    string Type,
    string ClientName,
    DateTime ScheduledAt,
    DateTime? ReadAt,
    DateTime CreatedAt
);
