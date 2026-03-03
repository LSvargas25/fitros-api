using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class GetWorkoutRoutineByIdTests
{
   

    [Fact]
    public async Task Should_Return_Routine_When_Exists()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var routine = WorkoutRoutine.Create("Push Day", "Chest routine");

        context.Add(routine);
        await context.SaveChangesAsync();

        var handler = new GetWorkoutRoutineByIdHandler(context);

        var query = new GetWorkoutRoutineByIdQuery(routine.Id);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(routine.Id);
        result.Name.Should().Be("Push Day");
        result.Version.Should().Be(1);
    }

    [Fact]
    public async Task Should_Return_Null_When_Not_Found()
    {
        // Arrange
        var context = TestDbContextFactory.Create();
        var handler = new GetWorkoutRoutineByIdHandler(context);

        var query = new GetWorkoutRoutineByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
