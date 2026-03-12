using FitRos.Application.Features.WeeklyTrainingPlans.AssignRoutineToDay;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Domain.Enums;
using FitRos.Tests.Helpers;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.WeeklyTrainingPlans;

public class AssignRoutineToDayTests
{
    [Fact]
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new AssignRoutineToDayHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(
                new AssignRoutineToDayCommand(Guid.NewGuid(), DayOfWeek.Monday, Guid.NewGuid(), null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Plan_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new AssignRoutineToDayHandler(context, fakeUser);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AssignRoutineToDayCommand(Guid.NewGuid(), DayOfWeek.Monday, Guid.NewGuid(), null),
                CancellationToken.None));
    }


  

 
}
