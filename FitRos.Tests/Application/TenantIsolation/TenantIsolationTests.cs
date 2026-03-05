using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.TenantIsolation
{
    public class TenantIsolationTests
    {
        [Fact]
        public async Task GymA_Should_Not_See_Data_From_GymB()
        {
            // Arrange

            var gymA = Guid.NewGuid();
            var gymB = Guid.NewGuid();

            var exercise = Exercise.Create(
                "Bench Press",
                "Chest exercise",
                MuscleGroup.Chest);

            exercise.GetType()
                .GetProperty("GymId")!
                .SetValue(exercise, gymA);

            using var context = TestDbContextFactory.Create();

            context.Exercises.Add(exercise);
            await context.SaveChangesAsync();

            // Act

            var currentUserGymB = new FakeCurrentUser(gymB)
            {
                IsAuthenticated = true,
                UserId = Guid.NewGuid()
            };

            using var contextGymB = TestDbContextFactory.Create(currentUserGymB);

            var exercises = await contextGymB.Exercises.ToListAsync();

            // Assert

            exercises.Should().BeEmpty();
        }

        [Fact]
        public async Task Gym_Should_See_Its_Own_Data()
        {
            var gymId = Guid.NewGuid();

            var user = new FakeCurrentUser(gymId)
            {
                UserId = Guid.NewGuid(),
                Role = UserRole.Admin,
                IsAuthenticated = true
            };

            var context = TestDbContextFactory.Create(user);

            var exercise = Exercise.Create(
                "Squat",
                "Leg exercise",
                MuscleGroup.Legs);

            // asignar GymId manualmente para el test
            exercise.GetType()
                .GetProperty("GymId")!
                .SetValue(exercise, gymId);

            context.Exercises.Add(exercise);
            await context.SaveChangesAsync();

            var exercises = await context.Exercises.ToListAsync();

            exercises.Should().HaveCount(1);
        }

    }
}

