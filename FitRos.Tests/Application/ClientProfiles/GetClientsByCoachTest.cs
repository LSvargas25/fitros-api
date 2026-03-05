using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.GetByCoach;
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles.GetByCoach
{
    public sealed class GetClientsByCoachTest
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
        public async Task Admin_should_get_clients_by_coach()
        {
            var gymId = Guid.NewGuid();
            var coachId = Guid.NewGuid();

            var client1 = ClientProfile.Create(gymId, Guid.NewGuid(), coachId);
            var client2 = ClientProfile.Create(gymId, Guid.NewGuid(), coachId);
            var otherClient = ClientProfile.Create(gymId, Guid.NewGuid(), Guid.NewGuid());

            var mock = new Mock<ICurrentUser>();
            mock.Setup(x => x.UserId).Returns(Guid.NewGuid());
            mock.Setup(x => x.Role).Returns(UserRole.Admin);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.GymId).Returns(gymId);

            using var context = CreateContext(mock.Object, client1, client2, otherClient);

            var handler = new GetClientsByCoachQueryHandler(context, mock.Object);

            var result = await handler.Handle(
                new GetClientsByCoachQuery(coachId),
                CancellationToken.None);

            result.Count.Should().Be(2);
        }
        [Fact]
        public async Task Coach_should_not_access_other_coach_clients()
        {
            var gymId = Guid.NewGuid();
            var realCoachId = Guid.NewGuid();
            var otherCoachId = Guid.NewGuid();

            var mock = new Mock<ICurrentUser>();
            mock.Setup(x => x.UserId).Returns(otherCoachId);
            mock.Setup(x => x.Role).Returns(UserRole.Coach);
            mock.Setup(x => x.IsAuthenticated).Returns(true);
            mock.Setup(x => x.GymId).Returns(gymId);

            using var context = CreateContext(mock.Object);

            var handler = new GetClientsByCoachQueryHandler(context, mock.Object);

            await Assert.ThrowsAsync<ForbiddenException>(() =>
                handler.Handle(
                    new GetClientsByCoachQuery(realCoachId),
                    CancellationToken.None));
        }
    }
}