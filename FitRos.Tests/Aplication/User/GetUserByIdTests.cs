using FitRos.Application.Features.Users.GetUserById;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Users;

public class GetUserByIdTests
{
    private static FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Return_User_When_Exists()
    {
        var context = CreateDbContext();

        var user = User.Create(
            "test@test.com",
            "Luis",
            "Vargas",
            "hash",
            UserRole.Client);

        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);

        var fakeCurrentUser = new FakeCurrentUser
        {
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
    public async Task Should_Return_Null_When_Not_Exists()
    {
        var context = CreateDbContext();

        var fakeCurrentUser = new FakeCurrentUser
        {
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new GetUserByIdHandler(context, fakeCurrentUser);

        var result = await handler.Handle(
            new GetUserByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Should_Not_Allow_If_Not_Authenticated()
    {
        var context = CreateDbContext();

        var fakeCurrentUser = new FakeCurrentUser
        {
            IsAuthenticated = false
        };

        var handler = new GetUserByIdHandler(context, fakeCurrentUser);

        var act = async () => await handler.Handle(
            new GetUserByIdQuery(Guid.NewGuid()),
            CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("You are not authorized.");
    }
}