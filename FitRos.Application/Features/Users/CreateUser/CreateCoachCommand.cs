using FitRos.Application.Features.Users.CreateUser;
using MediatR;

public sealed record CreateCoachCommand(
    string Email,
    string FirstName,
    string LastName,
    string Password
) : IRequest<CreateUserResponse>;