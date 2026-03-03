using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.SoftDelete;
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
    public sealed class SoftDeleteClientTest
    {
        private static FitRosDbContext CreateContext(params ClientProfile[] clients)
        {
            var options = new DbContextOptionsBuilder<FitRosDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = TestDbContextFactory.Create();
            context.ClientProfiles.AddRange(clients);
            context.SaveChanges();
            return context;
        }

        private static ICurrentUser MockUser(Guid? id, UserRole role)
        {
            var mock = new Mock<ICurrentUser>();
            mock.Setup(x => x.UserId).Returns(id);
            mock.Setup(x => x.Role).Returns(role);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            return mock.Object;
        }

        [Fact]
        public async Task Coach_should_soft_delete_own_client()
        {
            var coachId = Guid.NewGuid();
            var client = ClientProfile.Create(Guid.NewGuid(), coachId);

            using var context = CreateContext(client);
            var user = MockUser(coachId, UserRole.Coach);

            var handler = new SoftDeleteClientCommandHandler(context, user);

            await handler.Handle(
                new SoftDeleteClientCommand(client.Id),
                CancellationToken.None);

            client.Status.Should().Be(ClientStatus.Deleted);
            client.DeletedAt.Should().NotBeNull();
        }

        [Fact]
        public async Task Admin_should_soft_delete_any_client()
        {
            var client = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());

            using var context = CreateContext(client);
            var user = MockUser(Guid.NewGuid(), UserRole.Admin);

            var handler = new SoftDeleteClientCommandHandler(context, user);

            await handler.Handle(
                new SoftDeleteClientCommand(client.Id),
                CancellationToken.None);

            client.Status.Should().Be(ClientStatus.Deleted);
        }

        [Fact]
        public async Task Coach_should_not_delete_other_coach_client()
        {
            var client = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());

            using var context = CreateContext(client);
            var user = MockUser(Guid.NewGuid(), UserRole.Coach);

            var handler = new SoftDeleteClientCommandHandler(context, user);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new SoftDeleteClientCommand(client.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Should_throw_if_not_found()
        {
            using var context = CreateContext();
            var user = MockUser(Guid.NewGuid(), UserRole.Admin);

            var handler = new SoftDeleteClientCommandHandler(context, user);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                   handler.Handle(
                    new SoftDeleteClientCommand(Guid.NewGuid()),
                    CancellationToken.None));
        }
    }
}