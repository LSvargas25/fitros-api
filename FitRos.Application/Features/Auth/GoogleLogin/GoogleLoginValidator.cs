using FluentValidation;

namespace FitRos.Application.Features.Auth.GoogleLogin;

public sealed class GoogleLoginValidator : AbstractValidator<GoogleLoginCommand>
{
    public GoogleLoginValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty();
    }
}
