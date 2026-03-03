using FluentAssertions;
using FluentValidation.TestHelper;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;
using FitRos.Domain.Enums;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class GetWorkoutRoutinesTests
{
    private readonly GetWorkoutRoutinesValidator _validator;

    public GetWorkoutRoutinesTests()
    {
        _validator = new GetWorkoutRoutinesValidator();
    }

    [Fact]
    public void Should_Not_Have_Error_When_Status_Is_Null()
    {
        var query = new GetWorkoutRoutinesQuery(
            Status: null,
            Page: 1,
            PageSize: 10
        );

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Should_Not_Have_Error_When_Status_Is_Valid()
    {
        var query = new GetWorkoutRoutinesQuery(
            Status: RoutineStatus.Draft,
            Page: 1,
            PageSize: 10
        );

        var result = _validator.TestValidate(query);

        result.ShouldNotHaveValidationErrorFor(x => x.Status);
    }

    [Fact]
    public void Should_Have_Error_When_Status_Is_Invalid()
    {
        var invalidStatus = (RoutineStatus)999;

        var query = new GetWorkoutRoutinesQuery(
            Status: invalidStatus,
            Page: 1,
            PageSize: 10
        );

        var result = _validator.TestValidate(query);

        result.ShouldHaveValidationErrorFor(x => x.Status)
              .WithErrorMessage("Invalid routine status value.");
    }
}
