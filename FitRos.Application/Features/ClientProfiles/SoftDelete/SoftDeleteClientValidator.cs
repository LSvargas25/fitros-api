using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.SoftDelete
{
    public sealed class SoftDeleteClientCommandValidator
        : AbstractValidator<SoftDeleteClientCommand>
    {
        public SoftDeleteClientCommandValidator()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty();
        }
    }
}