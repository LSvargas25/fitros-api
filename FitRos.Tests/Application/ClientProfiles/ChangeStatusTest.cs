using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.ChangeClientStatus;

using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles
{
    public sealed class ChangeStatusTest
    {
        private static FitRosDbContext CreateContext(params ClientProfile[] clients)
        {
            var options = new DbContextOptionsBuilder<FitRosDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = TestDbContextFactory.Create();

            if (clients.Length > 0)
            {
                context.ClientProfiles.AddRange(clients);
                context.SaveChanges();
            }

            return context;
        }

        private static ICurrentUser MockUser(Guid? userId, UserRole role, Guid? gymId = null)
        {
            var mock = new Mock<ICurrentUser>();
            mock.Setup(x => x.UserId).Returns(userId);
            mock.Setup(x => x.Role).Returns(role);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.GymId).Returns(gymId);
            return mock.Object;
        }

        [Fact]
        public async Task Coach_should_activate_client()
        {
            var gymId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var coachId = Guid.NewGuid();

            var client = ClientProfile.Create(gymId, userId, coachId);
            client.Deactivate();

            using var context = CreateContext(client);
            var user = MockUser(coachId, UserRole.Coach, gymId);

            var handler = new ToggleClientStatusCommandHandler(context, user);

            await handler.Handle(
                new ToggleClientStatusCommand(client.Id, true),
                CancellationToken.None);

            client.Status.Should().Be(ClientStatus.Active);
        }

        [Fact]
        public async Task Coach_should_deactivate_client()
        {
            var gymId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var coachId = Guid.NewGuid();

            var client = ClientProfile.Create(gymId, userId, coachId);

            using var context = CreateContext(client);
            var user = MockUser(coachId, UserRole.Coach, gymId);

            var handler = new ToggleClientStatusCommandHandler(context, user);

            await handler.Handle(
                new ToggleClientStatusCommand(client.Id, false),
                CancellationToken.None);

            client.Status.Should().Be(ClientStatus.Inactive);
        }

        [Fact]
        public async Task Admin_should_modify_client_in_own_gym()
        {
            var gymId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var coachId = Guid.NewGuid();

            var client = ClientProfile.Create(gymId, userId, coachId);

            using var context = CreateContext(client);
            var user = MockUser(Guid.NewGuid(), UserRole.Admin, gymId);

            var handler = new ToggleClientStatusCommandHandler(context, user);

            await handler.Handle(
                new ToggleClientStatusCommand(client.Id, false),
                CancellationToken.None);

            client.Status.Should().Be(ClientStatus.Inactive);
        }

        [Fact]
        public async Task Admin_should_not_modify_client_from_other_gym()
        {
            var gymId = Guid.NewGuid();
            var otherGymId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var coachId = Guid.NewGuid();

            var client = ClientProfile.Create(gymId, userId, coachId);

            using var context = CreateContext(client);
            var user = MockUser(Guid.NewGuid(), UserRole.Admin, otherGymId);

            var handler = new ToggleClientStatusCommandHandler(context, user);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new ToggleClientStatusCommand(client.Id, false),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Coach_should_not_modify_other_coach_client()
        {
            var gymId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var coachId = Guid.NewGuid();

            var client = ClientProfile.Create(gymId, userId, coachId);
            client.Deactivate();

            using var context = CreateContext(client);
            var user = MockUser(Guid.NewGuid(), UserRole.Coach, gymId);

            var handler = new ToggleClientStatusCommandHandler(context, user);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new ToggleClientStatusCommand(client.Id, false),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Should_throw_if_client_not_found()
        {
            using var context = CreateContext();
            var user = MockUser(Guid.NewGuid(), UserRole.Admin);

            var handler = new ToggleClientStatusCommandHandler(context, user);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                  handler.Handle(
                    new ToggleClientStatusCommand(Guid.NewGuid(), true),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Should_throw_if_client_deleted()
        {
            var gymId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var coachId = Guid.NewGuid();

            var client = ClientProfile.Create(gymId, userId, coachId);
            client.SoftDelete();

            using var context = CreateContext(client);
            var user = MockUser(Guid.NewGuid(), UserRole.Admin);

            var handler = new ToggleClientStatusCommandHandler(context, user);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(
                    new ToggleClientStatusCommand(client.Id, true),
                    CancellationToken.None));
        }
    }
}