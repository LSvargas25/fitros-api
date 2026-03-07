using FluentValidation;

namespace FitRos.Application.Features.Users.GetUserById;

public sealed class GetUserByIdValidator
    : AbstractValidator<GetUserByIdQuery>
{
    public GetUserByIdValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty();
    }
}