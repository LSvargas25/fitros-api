using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Common;
using FitRos.Infrastructure.Persistence;

namespace FitRos.Tests.Application.WorkoutRoutines;

public class CreateWorkoutRoutineTests
{
    private FitRosDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Create_Routine_When_Name_Is_Unique()
    {
        var context = CreateContext();
        var handler = new CreateWorkoutRoutineHandler(context);

        var command = new CreateWorkoutRoutineCommand
        {
            Name = "Push Day",
            Description = "Chest routine"
        };

        var result = await handler.Handle(command, CancellationToken.None);

        result.Id.Should().NotBe(Guid.Empty);
        result.Name.Should().Be("Push Day");
        result.Version.Should().Be(1);

        (await context.WorkoutRoutines.CountAsync())
            .Should().Be(1);
    }

    [Fact]
    public async Task Should_Throw_When_Name_Already_Exists()
    {
        var context = CreateContext();

        var existing = WorkoutRoutine.Create("Push Day", "Chest routine");
        context.Add(existing);
        await context.SaveChangesAsync();

        var handler = new CreateWorkoutRoutineHandler(context);

        var command = new CreateWorkoutRoutineCommand
        {
            Name = "Push Day",
            Description = "Another description"
        };

        Func<Task> action = () =>
            handler.Handle(command, CancellationToken.None);

        await action.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("*already exists*");
    }
}
