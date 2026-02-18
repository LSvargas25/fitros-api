using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FluentAssertions;
using Xunit;
using System;
using FitRos.Domain.Entities.Enums;


namespace FitRos.Tests.Domain.Training
{
    public class ExerciseTests
    {
        [Fact]
        public void Should_Create_Exercise_With_Valid_Data()
        {
            var exercise = Exercise.Create(
                "Bench Press",
                "Chest compound movement",
                MuscleGroup.Chest);

            exercise.Name.Should().Be("Bench Press");
            exercise.NormalizedName.Should().Be("bench press");
            exercise.Description.Should().Be("Chest compound movement");
            exercise.Category.Should().Be(MuscleGroup.Chest);
            exercise.IsArchived.Should().BeFalse();
        }

        [Fact]
        public void Should_Not_Create_Exercise_With_Empty_Name()
        {
            var act = () => Exercise.Create(
                "",
                "desc",
                MuscleGroup.Chest);

            act.Should().Throw<ArgumentException>()
               .WithMessage("Exercise name cannot be empty.");
        }

        [Fact]
        public void Should_Update_Exercise_When_Not_Archived()
        {
            var exercise = Exercise.Create(
                "Bench Press",
                "Chest",
                MuscleGroup.Chest);

            exercise.Update(
                "Incline Bench Press",
                "Upper chest focus",
                MuscleGroup.Chest);

            exercise.Name.Should().Be("Incline Bench Press");
            exercise.NormalizedName.Should().Be("incline bench press");
            exercise.Description.Should().Be("Upper chest focus");
        }

        [Fact]
        public void Should_Not_Update_When_Archived()
        {
            var exercise = Exercise.Create(
                "Bench Press",
                "Chest",
                MuscleGroup.Chest);

            exercise.Archive();

            var act = () => exercise.Update(
                "New Name",
                "New Desc",
                MuscleGroup.Chest);

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Archived exercises cannot be modified.");
        }

        [Fact]
        public void Should_Archive_Exercise()
        {
            var exercise = Exercise.Create(
                "Bench Press",
                "Chest",
                MuscleGroup.Chest);

            exercise.Archive();

            exercise.IsArchived.Should().BeTrue();
        }

        [Fact]
        public void Should_Not_Archive_Twice()
        {
            var exercise = Exercise.Create(
                "Bench Press",
                "Chest",
                MuscleGroup.Chest);

            exercise.Archive();

            var act = () => exercise.Archive();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Exercise is already archived.");
        }

        [Fact]
        public void Should_Restore_Archived_Exercise()
        {
            var exercise = Exercise.Create(
                "Bench Press",
                "Chest",
                MuscleGroup.Chest);

            exercise.Archive();
            exercise.Restore();

            exercise.IsArchived.Should().BeFalse();
        }

        [Fact]
        public void Should_Not_Restore_If_Not_Archived()
        {
            var exercise = Exercise.Create(
                "Bench Press",
                "Chest",
                MuscleGroup.Chest);

            var act = () => exercise.Restore();

            act.Should().Throw<InvalidOperationException>()
               .WithMessage("Exercise is not archived.");
        }
    }
}
