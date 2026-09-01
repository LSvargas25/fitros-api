using FitRos.Application.Features.Auth.Login;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Auth;

public class LoginHandlerTests
{
    [Fact]
    public async Task Throws_When_Email_Not_Verified()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var hasher = new FakePasswordHasher();

        var user = User.CreateUnverified(
            "client@test.com", "A", "B", hasher.Hash("Password123!"), UserRole.Client);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new LoginHandler(context, hasher, new FakeTokenService());

        Func<Task> act = async () =>
            await handler.Handle(
                new LoginCommand("client@test.com", "Password123!"),
                CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Email not verified.");
    }

    [Fact]
    public async Task Succeeds_When_Email_Verified()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var hasher = new FakePasswordHasher();

        var user = User.Create(
            "client@test.com", "A", "B", hasher.Hash("Password123!"), UserRole.Client);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new LoginHandler(context, hasher, new FakeTokenService());

        var result = await handler.Handle(
            new LoginCommand("client@test.com", "Password123!"),
            CancellationToken.None);

        result.Email.Should().Be("client@test.com");
    }
}
