using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;
using FitRos.Domain.Common;
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

public class CreateWorkoutRoutineTests
{
   

    [Fact]
    public async Task Should_Create_Routine_When_Name_Is_Unique()
    {
        var context = TestDbContextFactory.Create();
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
        var context = TestDbContextFactory.Create();

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
