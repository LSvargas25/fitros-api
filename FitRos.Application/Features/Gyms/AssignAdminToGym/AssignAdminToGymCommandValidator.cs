using FluentValidation;

namespace FitRos.Application.Features.Gyms.AssignAdminToGym;

public sealed class AssignAdminToGymCommandValidator : AbstractValidator<AssignAdminToGymCommand>
{
    public AssignAdminToGymCommandValidator()
    {
        RuleFor(x => x.GymId).NotEmpty();
        RuleFor(x => x.AdminId).NotEmpty();
    }
}
