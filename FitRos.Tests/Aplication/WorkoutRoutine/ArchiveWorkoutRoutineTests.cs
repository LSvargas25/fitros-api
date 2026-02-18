using FluentAssertions;
using FluentValidation.TestHelper;
using FitRos.Application.Features.WorkoutRoutines.ArchiveWorkoutRoutine;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class ArchiveWorkoutRoutineTests
{
    private readonly ArchiveWorkoutRoutineValidator _validator;

    public ArchiveWorkoutRoutineTests()
    {
        _validator = new ArchiveWorkoutRoutineValidator();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Id_Is_Valid()
    {
        var command = new ArchiveWorkoutRoutineCommand(Guid.NewGuid());

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(x => x.Id);
    }

    [Fact]
    public void Should_Have_Error_When_Id_Is_Empty()
    {
        var command = new ArchiveWorkoutRoutineCommand(Guid.Empty);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("Workout routine id is required.");
    }
}
