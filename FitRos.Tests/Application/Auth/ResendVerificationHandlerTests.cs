using FitRos.Application.Features.Auth.ResendVerification;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Auth;

public class ResendVerificationHandlerTests
{
    [Fact]
    public async Task Issues_A_New_Code_For_An_Unverified_User()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var codeGenerator = new FakeEmailVerificationCodeGenerator();

        var user = User.CreateUnverified("client@test.com", "A", "B", "hash", UserRole.Client);
        user.SetEmailVerificationCode("STALE_HASH", DateTime.UtcNow.AddMinutes(-1));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new ResendVerificationHandler(context, codeGenerator, new FakeEmailSender());

        await handler.Handle(new ResendVerificationCommand("client@test.com"), CancellationToken.None);

        var reloaded = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == user.Id);
        reloaded.EmailVerificationCodeHash.Should().Be(codeGenerator.Hash("123456"));
    }

    [Fact]
    public async Task Does_Nothing_When_Already_Verified()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var codeGenerator = new FakeEmailVerificationCodeGenerator();

        var user = User.Create("client@test.com", "A", "B", "hash", UserRole.Client);
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new ResendVerificationHandler(context, codeGenerator, new FakeEmailSender());

        await handler.Handle(new ResendVerificationCommand("client@test.com"), CancellationToken.None);

        var reloaded = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == user.Id);
        reloaded.EmailVerificationCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task Does_Nothing_When_User_Does_Not_Exist()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var handler = new ResendVerificationHandler(
            context, new FakeEmailVerificationCodeGenerator(), new FakeEmailSender());

        await handler.Handle(new ResendVerificationCommand("nobody@test.com"), CancellationToken.None);
    }
}
