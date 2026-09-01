using FitRos.Application.Features.Users.UserManagement.UserList;
using FluentValidation;

namespace FitRos.Application.Features.Users.UserManagement.GetUsersAdvanced;

public sealed class GetUserByIdValidator
    : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}