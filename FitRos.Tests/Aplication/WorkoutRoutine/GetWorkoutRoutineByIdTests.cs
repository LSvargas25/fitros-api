using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class GetWorkoutRoutineByIdTests
{
    private FitRosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Return_Routine_When_Exists()
    {
        // Arrange
        var context = CreateContext();

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
        var context = CreateContext();
        var handler = new GetWorkoutRoutineByIdHandler(context);

        var query = new GetWorkoutRoutineByIdQuery(Guid.NewGuid());

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
}
