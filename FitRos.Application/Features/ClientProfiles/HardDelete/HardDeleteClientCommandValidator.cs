using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.HardDelete
{
    public sealed class HardDeleteClientCommandValidator
        : AbstractValidator<HardDeleteClientCommand>
    {
        public HardDeleteClientCommandValidator()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty();
        }
    }
}