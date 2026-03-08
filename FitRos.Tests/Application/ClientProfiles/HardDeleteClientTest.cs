using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.HardDelete;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles.HardDelete
{
    public sealed class HardDeleteClientTest
    {
        private static FitRosDbContext CreateContext(
            ICurrentUser currentUser,
            params ClientProfile[] clients)
        {
            var options = new DbContextOptionsBuilder<FitRosDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new FitRosDbContext(options, currentUser);

            context.ClientProfiles.AddRange(clients);
            context.SaveChanges();

            return context;
        }

        private static Mock<ICurrentUser> MockUser(Guid? id, UserRole role)
        {
            var mock = new Mock<ICurrentUser>();
            mock.Setup(x => x.UserId).Returns(id);
            mock.Setup(x => x.Role).Returns(role);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            return mock;
        }

        [Fact]
        public async Task Admin_should_hard_delete_soft_deleted_client()
        {
            var client = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());
            client.SoftDelete();

            var user = MockUser(Guid.NewGuid(), UserRole.Admin);

            using var context = CreateContext(user.Object, client);

            var handler = new HardDeleteClientCommandHandler(context, user.Object);

            await handler.Handle(
                new HardDeleteClientCommand(client.Id),
                CancellationToken.None);

            context.ClientProfiles
                .IgnoreQueryFilters()
                .Any(x => x.Id == client.Id)
                .Should()
                .BeFalse();
        }

        [Fact]
        public async Task Should_throw_if_not_soft_deleted()
        {
            var client = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());

            var user = MockUser(Guid.NewGuid(), UserRole.Admin);

            using var context = CreateContext(user.Object, client);

            var handler = new HardDeleteClientCommandHandler(context, user.Object);

            await Assert.ThrowsAsync<DomainException>(() =>
                handler.Handle(
                    new HardDeleteClientCommand(client.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Should_throw_if_not_admin()
        {
            var client = ClientProfile.Create(Guid.NewGuid(), Guid.NewGuid());
            client.SoftDelete();

            var user = MockUser(Guid.NewGuid(), UserRole.Coach);

            using var context = CreateContext(user.Object, client);

            var handler = new HardDeleteClientCommandHandler(context, user.Object);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new HardDeleteClientCommand(client.Id),
                    CancellationToken.None));
        }

        [Fact]
        public async Task Should_throw_if_not_found()
        {
            var user = MockUser(Guid.NewGuid(), UserRole.Admin);

            using var context = CreateContext(user.Object);

            var handler = new HardDeleteClientCommandHandler(context, user.Object);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                     handler.Handle(
                    new HardDeleteClientCommand(Guid.NewGuid()),
                    CancellationToken.None));
        }
    }
}