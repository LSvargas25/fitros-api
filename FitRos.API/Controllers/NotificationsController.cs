using FitRos.Application.Features.Notifications.DeleteNotification;
using FitRos.Application.Features.Notifications.GetNotifications;
using FitRos.Application.Features.Notifications.GetUnreadNotifications;
using FitRos.Application.Features.Notifications.MarkNotificationAsRead;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public sealed class NotificationsController : ControllerBase
{
    private const string TagNotifications = "Notifications";

    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    // GET /api/notifications
    [HttpGet]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get notifications",
        Description = "Returns all notifications for the authenticated user.",
        Tags = new[] { TagNotifications })]
    public async Task<ActionResult<List<NotificationDto>>> GetNotifications(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetNotificationsQuery(), cancellationToken);
        return Ok(result);
    }

    // GET /api/notifications/unread
    [HttpGet("unread")]
    [ProducesResponseType(typeof(List<NotificationDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get unread notifications",
        Description = "Returns all unread notifications for the authenticated user.",
        Tags = new[] { TagNotifications })]
    public async Task<ActionResult<List<NotificationDto>>> GetUnreadNotifications(
        CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetUnreadNotificationsQuery(), cancellationToken);
        return Ok(result);
    }

    // PATCH /api/notifications/{id}/read
    [HttpPatch("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Mark notification as read",
        Description = "Marks a specific notification as read.",
        Tags = new[] { TagNotifications })]
    public async Task<IActionResult> MarkAsRead(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new MarkNotificationAsReadCommand(id), cancellationToken);
        return NoContent();
    }

    // DELETE /api/notifications/{id}
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [SwaggerOperation(
        Summary = "Delete notification",
        Description = "Permanently removes a notification.",
        Tags = new[] { TagNotifications })]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteNotificationCommand(id), cancellationToken);
        return NoContent();
    }
}
