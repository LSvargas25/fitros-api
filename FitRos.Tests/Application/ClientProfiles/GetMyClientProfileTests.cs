using System;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.GetMyClientProfile;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles;

public class GetMyClientProfileTests
{
    private static FitRosDbContext CreateContext(ICurrentUser currentUser)
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options, currentUser);
    }

    private static Mock<ICurrentUser> CreateCurrentUser(
        bool isAuthenticated,
        UserRole role,
        Guid? userId,
        Guid? gymId = null)
    {
        var mock = new Mock<ICurrentUser>();

        mock.Setup(x => x.IsAuthenticated).Returns(isAuthenticated);
        mock.Setup(x => x.Role).Returns(role);
        mock.Setup(x => x.UserId).Returns(userId);
        mock.Setup(x => x.GymId).Returns(gymId);

        return mock;
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Authenticated()
    {
        var currentUser = CreateCurrentUser(false, UserRole.Client, Guid.NewGuid());

        using var context = CreateContext(currentUser.Object);

        var handler = new GetMyClientProfileQueryHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(new GetMyClientProfileQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Client_Has_No_Profile()
    {
        var currentUser = CreateCurrentUser(true, UserRole.Client, Guid.NewGuid());

        using var context = CreateContext(currentUser.Object);

        var handler = new GetMyClientProfileQueryHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new GetMyClientProfileQuery(), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Return_Own_Profile_With_Measures()
    {
        var gymId = Guid.NewGuid();
        var coachId = Guid.NewGuid();
        var clientUserId = Guid.NewGuid();

        var currentUser = CreateCurrentUser(true, UserRole.Client, clientUserId, gymId);

        using var context = CreateContext(currentUser.Object);

        var profile = ClientProfile.Create(gymId, clientUserId, coachId);
        profile.AddMeasure(80, 20, 30, 90, 100, 40);
        context.ClientProfiles.Add(profile);
        await context.SaveChangesAsync();

        var handler = new GetMyClientProfileQueryHandler(context, currentUser.Object);

        var result = await handler.Handle(new GetMyClientProfileQuery(), CancellationToken.None);

        Assert.Equal(profile.Id, result.Id);
        Assert.Single(result.Measures);
    }
}
