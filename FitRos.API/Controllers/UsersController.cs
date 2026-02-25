using FitRos.Application.Features.Users.ActivateUser;
using FitRos.Application.Features.Users.CreateUser;
using FitRos.Application.Features.Users.DeactivateUser;
using FitRos.Application.Features.Users.DeleteUser;
using FitRos.Application.Features.Users.GetUserById;
using FitRos.Application.Features.Users.GetUsersAdvanced;
using FitRos.Application.Features.Users.UpdateUser;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController : ControllerBase
{
    private const string TagCore = "Users - Core";

    private readonly ISender _sender;

    public UsersController(ISender sender)
    {
        _sender = sender;
    }

    // =============================
    // Create Client
    // =============================

    [HttpPost("clients")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create client",
        Description = "Creates a new Client user.",
        Tags = new[] { TagCore })]
    public async Task<ActionResult<CreateUserResponse>> CreateClient(
    [FromBody] CreateClientCommand command,
    CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // =============================
    // Create Coach
    // =============================

    [HttpPost("coaches")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create coach",
        Description = "Creates a new Coach user.",
        Tags = new[] { TagCore })]
    public async Task<ActionResult<CreateUserResponse>> CreateCoach(
        [FromBody] CreateCoachCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    // =============================
    // Update
    // =============================

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Update user",
        Description = "Updates basic user information.",
        Tags = new[] { TagCore })]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest body,
        CancellationToken ct)
    {
        var command = new UpdateUserCommand(
            id,
            body.FirstName,
            body.LastName,
            body.Email,
            body.Role.HasValue ? (UserRole?)body.Role.Value : null
        );

        var result = await _sender.Send(command, ct);

        return Ok(result);
    }

    // =============================
    // Get by Id
    // =============================

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get user by id",
        Description = "Retrieves a user by its unique identifier.",
        Tags = new[] { TagCore })]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetUserByIdQuery(id), ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    // =============================
    // Soft Delete
    // =============================

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Deactivate user",
        Description = "Soft deletes a user.",
        Tags = new[] { TagCore })]
    public async Task<IActionResult> Deactivate(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeactivateUserCommand(id), ct);
        return NoContent();
    }

    // =============================
    // Activate
    // =============================

    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Activate user",
        Description = "Reactivates a user.",
        Tags = new[] { TagCore })]
    public async Task<IActionResult> Activate(Guid id, CancellationToken ct)
    {
        await _sender.Send(new ActivateUserCommand(id), ct);
        return NoContent();
    }

    // =============================
    // Hard Delete
    // =============================

    [HttpDelete("{id:guid}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [SwaggerOperation(
        Summary = "Hard delete user",
        Description = "Permanently deletes a user.",
        Tags = new[] { TagCore })]
    public async Task<IActionResult> HardDelete(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteUserCommand(id), ct);
        return NoContent();
    }

    // =============================
    // Advanced Query
    // =============================

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get users (advanced)",
        Description = "Advanced users search with cursor pagination.",
        Tags = new[] { TagCore })]
    public async Task<IActionResult> GetUsersAdvanced(
        [FromQuery] GetUsersAdvancedQuery query,
        CancellationToken ct)
    {
        var result = await _sender.Send(query, ct);

        Response.Headers.Add("X-Next-Cursor", result.NextCursor ?? string.Empty);

        return Ok(result);
    }
}