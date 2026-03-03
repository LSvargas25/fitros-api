using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.ReassignCoach;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
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

        private static ICurrentUser MockUser(Guid? id, UserRole role)
        {
            var mock = new Mock<ICurrentUser>();
            mock.Setup(x => x.UserId).Returns(id);
            mock.Setup(x => x.Role).Returns(role);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            return mock.Object;
        }

        

        
        [Fact]
        public async Task Should_throw_if_client_not_found()
        {
            var adminUser = MockUser(Guid.NewGuid(), UserRole.Admin);

            using var context = CreateContext(adminUser);

            var handler = new ReassignClientCoachCommandHandler(context, adminUser);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                  handler.Handle(
                    new ReassignClientCoachCommand(Guid.NewGuid(), Guid.NewGuid()),
                    CancellationToken.None));
        }
    }
}