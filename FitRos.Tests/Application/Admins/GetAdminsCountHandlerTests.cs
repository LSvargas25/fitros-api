using FitRos.Application.Features.Users.Admin.GetAdminsCount;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Admins;

public class GetAdminsCountHandlerTests
{
    [Fact]
    public async Task OwnerApp_Gets_Total_Admin_Count()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);

        context.Users.Add(User.Create("admin1@test.com", "A", "One", "hash", UserRole.Admin));
        context.Users.Add(User.Create("admin2@test.com", "B", "Two", "hash", UserRole.Admin));
        var gymId = Guid.NewGuid();
        context.Users.Add(User.CreateForGym(gymId, "coach@test.com", "C", "Coach", "hash", UserRole.Coach));
        await context.SaveChangesAsync();

        var handler = new GetAdminsCountHandler(context);
        var result = await handler.Handle(new GetAdminsCountQuery(), CancellationToken.None);

        result.Should().Be(2);
    }

    [Fact]
    public async Task Returns_Zero_When_No_Admins()
    {
        var fakeOwner = new FakeCurrentUser(null)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeOwner);
        var handler = new GetAdminsCountHandler(context);

        var result = await handler.Handle(new GetAdminsCountQuery(), CancellationToken.None);

        result.Should().Be(0);
    }
}
