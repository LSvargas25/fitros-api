using FitRos.Application.Features.Users.UserManagement.UserList;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Users;

public class GetUserByIdTests
{
    [Fact]
    public async Task Should_Return_User_When_Exists()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var user = User.Create(
            "test@test.com",
            "Luis",
            "Vargas",
            "hash",
            UserRole.Client);

        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new GetUserByIdHandler(context, fakeCurrentUser);

        var result = await handler.Handle(
            new GetUserByIdQuery(user.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result!.Email.Should().Be("test@test.com");
    }

    [Fact]
    public async Task Should_Throw_NotFound_When_User_Does_Not_Exist()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new GetUserByIdHandler(context, fakeCurrentUser);

        await FluentActions.Invoking(() =>
                handler.Handle(
                    new GetUserByIdQuery(Guid.NewGuid()),
                    CancellationToken.None))
            .Should()
            .ThrowAsync<NotFoundException>()
            .WithMessage("User not found.");
    }

    [Fact]
    public async Task Should_Not_Allow_If_Not_Authenticated()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            IsAuthenticated = false
        };

        var handler = new GetUserByIdHandler(context, fakeCurrentUser);

        var act = async () => await handler.Handle(
            new GetUserByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<UnauthorizedException>()
            .WithMessage("User not authenticated.");
    }
}