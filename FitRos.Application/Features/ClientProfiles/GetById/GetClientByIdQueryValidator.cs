using FluentValidation;

namespace FitRos.Application.Features.ClientProfiles.GetById
{
    public sealed class GetClientByIdQueryValidator
        : AbstractValidator<GetClientByIdQuery>
    {
        public GetClientByIdQueryValidator()
        {
            RuleFor(x => x.ClientId)
                .NotEmpty();
        }
    }
}