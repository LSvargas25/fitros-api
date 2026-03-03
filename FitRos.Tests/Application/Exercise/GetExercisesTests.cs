using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.Exercises.GetExercises;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Exercises;

public class GetExercisesHandlerTests
{
 
    [Fact]
    public async Task Should_Return_All_Active_Exercises_When_No_Filter()
    {
        // Arrange
        var context = TestDbContextFactory.Create();

        var chest = Exercise.Create("Bench Press", "Chest", MuscleGroup.Chest);
        var legs = Exercise.Create("Squat", "Legs", MuscleGroup.Legs);
        var archived = Exercise.Create("Deadlift", "Back", MuscleGroup.Back);

        archived.Archive();

        context.AddRange(chest, legs, archived);
        await context.SaveChangesAsync(CancellationToken.None);

        IFitRosDbContext abstraction = context;
        var handler = new GetExercisesHandler(abstraction);

        // Act
        var result = await handler.Handle(
            new GetExercisesQuery(null),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(x => x.Name == "Bench Press");
        result.Should().Contain(x => x.Name == "Squat");
        result.Should().NotContain(x => x.Name == "Deadlift");
    }

    [Fact]
    public async Task Should_Filter_By_Category_When_Category_Is_Provided()
    {
        // Arrange
        var context = TestDbContextFactory.Create();

        var chest1 = Exercise.Create("Bench Press", "Chest", MuscleGroup.Chest);
        var chest2 = Exercise.Create("Incline Press", "Chest", MuscleGroup.Chest);
        var legs = Exercise.Create("Squat", "Legs", MuscleGroup.Legs);

        context.AddRange(chest1, chest2, legs);
        await context.SaveChangesAsync(CancellationToken.None);

        IFitRosDbContext abstraction = context;
        var handler = new GetExercisesHandler(abstraction);

        // Act
        var result = await handler.Handle(
            new GetExercisesQuery(MuscleGroup.Chest),
            CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(x => x.Category == MuscleGroup.Chest);
    }
}
