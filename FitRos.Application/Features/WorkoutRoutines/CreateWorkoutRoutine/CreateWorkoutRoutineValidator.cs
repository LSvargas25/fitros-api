using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;


namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine
{
    public class CreateWorkoutRoutineValidator
        : AbstractValidator<CreateWorkoutRoutineCommand>
    {
        public CreateWorkoutRoutineValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty()
                .WithMessage("Routine name is required.")
                .MinimumLength(3)
                .WithMessage("Routine name must be at least 3 characters.")
                .MaximumLength(100)
                .WithMessage("Routine name must not exceed 100 characters.");

            RuleFor(x => x.Description)
                .MaximumLength(500)
                .WithMessage("Description must not exceed 500 characters.");
        }
    }
}