using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.ReassignCoach;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles.ReassignCoach
{
    public sealed class ReassignClientCoachTest
    {
        private static FitRosDbContext CreateContext(ICurrentUser currentUser)
        {
            var options = new DbContextOptionsBuilder<FitRosDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new FitRosDbContext(options, currentUser);
        }

        private static ICurrentUser MockUser(UserRole role, Guid? gymId)
        {
            var mock = new Mock<ICurrentUser>();
            mock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            mock.Setup(x => x.Role).Returns(role);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.GymId).Returns(gymId);
            return mock.Object;
        }

        private static User Coach(Guid gymId)
        {
            var u = User.Create($"{Guid.NewGuid():N}@fitros.com", "C", "H", "hash", UserRole.Coach);
            u.AssignToGym(gymId);
            return u;
        }

        [Fact]
        public async Task Should_throw_if_client_not_found()
        {
            var admin = MockUser(UserRole.Admin, Guid.NewGuid());
            using var context = CreateContext(admin);
            var handler = new ReassignClientCoachCommandHandler(context, admin);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                handler.Handle(
                    new ReassignClientCoachCommand(Guid.NewGuid(), Guid.NewGuid()),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Admin_can_reassign_a_client_to_a_coach_in_the_same_gym()
        {
            var gymId = Guid.NewGuid();
            var admin = MockUser(UserRole.Admin, gymId);
            using var context = CreateContext(admin);

            var client = ClientProfile.Create(gymId, Guid.NewGuid(), Guid.NewGuid());
            var newCoach = Coach(gymId);
            context.ClientProfiles.Add(client);
            context.Users.Add(newCoach);
            await context.SaveChangesAsync();

            var handler = new ReassignClientCoachCommandHandler(context, admin);

            await handler.Handle(
                new ReassignClientCoachCommand(client.Id, newCoach.Id),
                CancellationToken.None);

            (await context.ClientProfiles.IgnoreQueryFilters().FirstAsync(c => c.Id == client.Id))
                .CoachId.Should().Be(newCoach.Id);
        }

        [Fact]
        public async Task Admin_cannot_reassign_a_client_in_another_gym()
        {
            var admin = MockUser(UserRole.Admin, Guid.NewGuid());
            using var context = CreateContext(admin);

            var client = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
            var newCoach = Coach(Guid.NewGuid());
            context.ClientProfiles.Add(client);
            context.Users.Add(newCoach);
            await context.SaveChangesAsync();

            var handler = new ReassignClientCoachCommandHandler(context, admin);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new ReassignClientCoachCommand(client.Id, newCoach.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Cannot_reassign_to_a_coach_from_a_different_gym()
        {
            var gymId = Guid.NewGuid();
            var admin = MockUser(UserRole.Admin, gymId);
            using var context = CreateContext(admin);

            var client = ClientProfile.Create(gymId, Guid.NewGuid(), Guid.NewGuid());
            var foreignCoach = Coach(Guid.NewGuid()); // different gym
            context.ClientProfiles.Add(client);
            context.Users.Add(foreignCoach);
            await context.SaveChangesAsync();

            var handler = new ReassignClientCoachCommandHandler(context, admin);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(
                    new ReassignClientCoachCommand(client.Id, foreignCoach.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Throws_if_target_user_is_not_a_coach()
        {
            var gymId = Guid.NewGuid();
            var admin = MockUser(UserRole.Admin, gymId);
            using var context = CreateContext(admin);

            var client = ClientProfile.Create(gymId, Guid.NewGuid(), Guid.NewGuid());
            var notACoach = User.Create($"{Guid.NewGuid():N}@fitros.com", "N", "C", "hash", UserRole.Client);
            notACoach.AssignToGym(gymId);
            context.ClientProfiles.Add(client);
            context.Users.Add(notACoach);
            await context.SaveChangesAsync();

            var handler = new ReassignClientCoachCommandHandler(context, admin);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(
                    new ReassignClientCoachCommand(client.Id, notACoach.Id),
                    CancellationToken.None));
        }
    }
}
