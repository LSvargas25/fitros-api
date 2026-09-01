using FitRos.Application.Features.Auth.VerifyEmail;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Auth;

public class VerifyEmailHandlerTests
{
    [Fact]
    public async Task Marks_User_Verified_With_Valid_Code()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var codeGenerator = new FakeEmailVerificationCodeGenerator();

        var user = User.CreateUnverified("client@test.com", "A", "B", "hash", UserRole.Client);
        user.SetEmailVerificationCode(codeGenerator.Hash("123456"), DateTime.UtcNow.AddMinutes(15));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new VerifyEmailHandler(context, codeGenerator);

        await handler.Handle(new VerifyEmailCommand("client@test.com", "123456"), CancellationToken.None);

        var reloaded = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == user.Id);
        reloaded.EmailVerified.Should().BeTrue();
        reloaded.EmailVerificationCodeHash.Should().BeNull();
    }

    [Fact]
    public async Task Throws_When_Code_Is_Wrong()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var codeGenerator = new FakeEmailVerificationCodeGenerator();

        var user = User.CreateUnverified("client@test.com", "A", "B", "hash", UserRole.Client);
        user.SetEmailVerificationCode(codeGenerator.Hash("123456"), DateTime.UtcNow.AddMinutes(15));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new VerifyEmailHandler(context, codeGenerator);

        Func<Task> act = async () =>
            await handler.Handle(new VerifyEmailCommand("client@test.com", "000000"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }

    [Fact]
    public async Task Throws_When_Code_Expired()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var codeGenerator = new FakeEmailVerificationCodeGenerator();

        var user = User.CreateUnverified("client@test.com", "A", "B", "hash", UserRole.Client);
        user.SetEmailVerificationCode(codeGenerator.Hash("123456"), DateTime.UtcNow.AddMinutes(-1));
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var handler = new VerifyEmailHandler(context, codeGenerator);

        Func<Task> act = async () =>
            await handler.Handle(new VerifyEmailCommand("client@test.com", "123456"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
