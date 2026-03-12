using FitRos.Application.Features.Users.Admin.ActivateAdmin;
using FitRos.Application.Features.Users.Admin.CreateAdmin;
using FitRos.Application.Features.Users.Admin.DeleteAdmin;
using FitRos.Application.Features.Users.Admin.DesactivateAdmin;
using FitRos.Application.Features.Users.Admin.GetAdminById;
using FitRos.Application.Features.Users.Admin.GetAdmins;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/admins")]
public sealed class AdminsController : ControllerBase
{
    private const string TagAdmins = "Admins";

    private readonly ISender _sender;

    public AdminsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create admin",
        Description = "Creates a new Admin user (without gym). OwnerApp only.",
        Tags = new[] { TagAdmins })]
    public async Task<ActionResult<CreateUserResponse>> CreateAdmin(
        [FromBody] CreateAdminCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AdminListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "List admins",
        Description = "Returns all admin users with gym info. OwnerApp only.",
        Tags = new[] { TagAdmins })]
    public async Task<IActionResult> GetAdmins(CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AdminDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get admin by id",
        Description = "Returns admin details including assigned gym. OwnerApp only.",
        Tags = new[] { TagAdmins })]
    public async Task<IActionResult> GetAdminById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetAdminByIdQuery(id), ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Activate admin",
        Description = "Reactivates an inactive admin user. OwnerApp only.",
        Tags = new[] { TagAdmins })]
    public async Task<IActionResult> ActivateAdmin(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ActivateAdminCommand(id), ct);
        return NoContent();
    }

    [HttpPatch("{id:guid}/deactivate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Deactivate admin",
        Description = "Deactivates an active admin user. OwnerApp only.",
        Tags = new[] { TagAdmins })]
    public async Task<IActionResult> DeactivateAdmin(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DesactivateAdminCommand(id), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Hard delete admin",
        Description = "Permanently deletes an admin. Admin must be inactive first. OwnerApp only.",
        Tags = new[] { TagAdmins })]
    public async Task<IActionResult> DeleteAdmin(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteAdminCommand(id), ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Hard delete admin (permanent)",
        Description = "Permanently deletes an admin. Admin must be inactive first. OwnerApp only.",
        Tags = new[] { TagAdmins })]
    public async Task<IActionResult> DeleteAdminPermanent(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteAdminCommand(id), ct);
        return NoContent();
    }
}
