using FitRos.Application.Features.Users.ActivateUser;
using FitRos.Application.Features.Users.CreateUser;
using FitRos.Application.Features.Users.DeactivateUser;
using FitRos.Application.Features.Users.DeleteUser;
using FitRos.Application.Features.Users.GetUserById;
using FitRos.Application.Features.Users.GetUsersAdvanced;
using FitRos.Application.Features.Users.UpdateUser;
using FitRos.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private const string TagCore = "Users - Core";

    /// Creates a new Client user.
    [HttpPost("clients")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create client",
        Description = "Creates a new Client user.",
        Tags = new[] { TagCore }
    )]
    public async Task<ActionResult<CreateUserResponse>> CreateClient(
        [FromBody] CreateUserCommand command,
        [FromServices] CreateUserHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            command,
            UserRole.Client,
            cancellationToken);

        return CreatedAtAction(nameof(CreateClient), new { id = result.Id }, result);
    }
    /// Updates basic user information.
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update user",
        Description = "Updates first name, last name, optionally email (Admin only) and role (Admin only).",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateUserRequest body,
        [FromServices] UpdateUserHandler handler,
        CancellationToken cancellationToken)
    {
        var command = new UpdateUserCommand(
            Id: id,
            FirstName: body.FirstName,
            LastName: body.LastName,
            Email: body.Email,
            Role: body.Role.HasValue ? (UserRole)body.Role.Value : null
        );

        var result = await handler.Handle(command, cancellationToken);

        return Ok(result);
    }

    /// Creates a new Coach user (Admin only).
    [HttpPost("coaches")]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create coach",
        Description = "Creates a new Coach user. Only Admin allowed.",
        Tags = new[] { TagCore }
    )]
    public async Task<ActionResult<CreateUserResponse>> CreateCoach(
        [FromBody] CreateUserCommand command,
        [FromServices] CreateUserHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            command,
            UserRole.Coach,
            cancellationToken);

        return CreatedAtAction(nameof(CreateCoach), new { id = result.Id }, result);
    }

    /// Retrieves a user by id.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get user by id",
        Description = "Retrieves a user by its unique identifier.",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> GetById(
        Guid id,
        [FromServices] GetUserByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.Handle(
            new GetUserByIdQuery(id),
            cancellationToken);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    /// Soft deletes a user by setting Status = Inactive.
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Deactivate user",
        Description = "Soft deletes a user by setting Status = Inactive.",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> Deactivate(
        Guid id,
        [FromServices] DeactivateUserHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new DeactivateUserCommand(id),
            cancellationToken);

        return NoContent();
    }

    /// Advanced users search with cursor pagination, sorting, filters and incremental search.
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get users (advanced)",
        Description = "Cursor pagination with sorting, filters, and incremental search.",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> GetUsersAdvanced(
    [FromServices] GetUsersAdvancedHandler handler,
    CancellationToken cancellationToken,
    [FromQuery] GetUsersAdvancedQuery query)
    {
        var result = await handler.Handle(query, cancellationToken);

        Response.Headers.Add("X-Next-Cursor", result.NextCursor ?? string.Empty);

        return Ok(result);
    }
    //activate a user by setting Status = Active

    /// Reactivates a previously deactivated user.
    [HttpPatch("{id:guid}/activate")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Activate user",
        Description = "Reactivates a previously deactivated user by setting Status = Active.",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> Activate(
        Guid id,
        [FromServices] ActivateUserHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new ActivateUserCommand(id),
            cancellationToken);

        return NoContent();
    }
    // Permanently deletes a user from the database. Only Admin can perform this action and only on Inactive users.
    /// Permanently deletes a user (Hard Delete).
    [HttpDelete("{id:guid}/permanent")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Hard delete user",
        Description = "Permanently deletes a user. Admin can delete Coach and Client. Coach can delete Client only.",
        Tags = new[] { TagCore }
    )]
    public async Task<IActionResult> HardDelete(
        Guid id,
        [FromServices] DeleteUserHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.Handle(
            new DeleteUserCommand(id),
            cancellationToken);

        return NoContent();
    }

}