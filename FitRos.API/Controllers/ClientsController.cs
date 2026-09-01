using FitRos.Application.Features.Users.Client.CreateClient;
using FitRos.Application.Features.Users.Client.DeleteClient;
using FitRos.Application.Features.Users.Client.GetClientById;
using FitRos.Application.Features.Users.Client.GetClients;
using FitRos.Application.Features.Users.Client.GetClientsCount;
using FitRos.Application.Features.Users.Client.UpdateClient;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FitRos.API.Controllers;

[ApiController]
[Route("api/clients")]
public sealed class ClientsController : ControllerBase
{
    private const string TagClients = "Clients";

    private readonly ISender _sender;

    public ClientsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(CreateUserResponse), StatusCodes.Status201Created)]
    [SwaggerOperation(
        Summary = "Create client",
        Description = "Creates a new Client user. Coach inherits own GymId and CoachId automatically.",
        Tags = new[] { TagClients })]
    public async Task<ActionResult<CreateUserResponse>> CreateClient(
        [FromBody] CreateClientCommand command,
        CancellationToken ct)
    {
        var result = await _sender.Send(command, ct);
        return CreatedAtAction(nameof(UsersController.GetById), "Users", new { id = result.Id }, result);
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<ClientUserListItemDto>), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "List clients",
        Description = "Returns clients scoped by role: OwnerApp=all, Admin=own gym, Coach=own clients.",
        Tags = new[] { TagClients })]
    public async Task<IActionResult> GetClients(CancellationToken ct)
    {
        var result = await _sender.Send(new GetClientsQuery(), ct);
        return Ok(result);
    }

    [HttpGet("total")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [SwaggerOperation(
        Summary = "Get clients count",
        Description = "Returns total client count. Admin scoped to own gym.",
        Tags = new[] { TagClients })]
    public async Task<IActionResult> GetClientsTotal(CancellationToken ct)
    {
        var count = await _sender.Send(new GetClientsCountQuery(), ct);
        return Ok(count);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ClientUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Get client by id",
        Description = "Returns client detail. Coaches can only see their own clients.",
        Tags = new[] { TagClients })]
    public async Task<IActionResult> GetClientById(Guid id, CancellationToken ct)
    {
        var result = await _sender.Send(new GetClientByIdQuery(id), ct);

        if (result is null)
            return NotFound();

        return Ok(result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ClientUserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Update client",
        Description = "Updates client name/email. OwnerApp=any, Admin=own gym, Coach=own clients.",
        Tags = new[] { TagClients })]
    public async Task<IActionResult> UpdateClient(Guid id, [FromBody] UpdateClientCommand command, CancellationToken ct)
    {
        var result = await _sender.Send(command with { ClientId = id }, ct);
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [SwaggerOperation(
        Summary = "Hard delete client",
        Description = "Permanently deletes an inactive client. OwnerApp=any, Admin=own gym, Coach=own clients.",
        Tags = new[] { TagClients })]
    public async Task<IActionResult> DeleteClient(Guid id, CancellationToken ct)
    {
        await _sender.Send(new DeleteClientCommand(id), ct);
        return NoContent();
    }
}
