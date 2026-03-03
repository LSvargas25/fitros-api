using FitRos.Application.Features.Auth.ForgotPassword;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Auth;

public sealed class ForgotPasswordTests
{
    //[Fact]
    //public async Task Should_Generate_Reset_Token_When_User_Exists()
    //{
    //   var context = TestDbContextFactory.Create();

    //    var user = User.Create(
    //        "user@test.com",
    //        "John",
    //        "Doe",
    //        "hashedPassword",
    //        UserRole.Client);

    //    context.Users.Add(user);
    //    await context.SaveChangesAsync();

    //    var tokenGenerator = new FakeResetTokenGenerator();
    //    var handler = new ForgotPasswordHandler(context, tokenGenerator);

    //    await handler.Handle(
    //        new ForgotPasswordCommand("user@test.com"),
    //        CancellationToken.None);

    //    user.PasswordResetTokenHash.Should().NotBeNull();
    //    user.PasswordResetTokenExpiresAtUtc.Should().NotBeNull();
    //}

    //[Fact]
    //public async Task Should_Not_Throw_When_User_Does_Not_Exist()
    //{
    //   var context = TestDbContextFactory.Create();

    //    var handler = new ForgotPasswordHandler(
    //        context,
    //        new FakeResetTokenGenerator());

    //    var act = async () =>
    //        await handler.Handle(
    //            new ForgotPasswordCommand("notfound@test.com"),
    //            CancellationToken.None);

    //    await act.Should().NotThrowAsync();
    //}

    //[Fact]
    //public async Task Should_Normalize_Email()
    //{
    //   var context = TestDbContextFactory.Create();

    //    var user = User.Create(
    //        "user@test.com",
    //        "John",
    //        "Doe",
    //        "hashedPassword",
    //        UserRole.Client);

    //    context.Users.Add(user);
    //    await context.SaveChangesAsync();

    //    var handler = new ForgotPasswordHandler(
    //        context,
    //        new FakeResetTokenGenerator());

    //    await handler.Handle(
    //        new ForgotPasswordCommand("  USER@TEST.COM  "),
    //        CancellationToken.None);

    //    var savedUser = await context.Users.FirstAsync();

    //    // Ensure lookup worked with normalized email
    //    savedUser.PasswordResetTokenHash.Should().NotBeNull();
    //}

    //[Fact]
    //public async Task Should_Overwrite_Previous_Reset_Token()
    //{
    //   var context = TestDbContextFactory.Create();

    //    var user = User.Create(
    //        "user@test.com",
    //        "John",
    //        "Doe",
    //        "hashedPassword",
    //        UserRole.Client);

    //    user.SetPasswordResetToken("old-hash", DateTime.UtcNow.AddMinutes(10));

    //    context.Users.Add(user);
    //    await context.SaveChangesAsync();

    //    var handler = new ForgotPasswordHandler(
    //        context,
    //        new FakeResetTokenGenerator());

    //    await handler.Handle(
    //        new ForgotPasswordCommand("user@test.com"),
    //        CancellationToken.None);

    //    var savedUser = await context.Users.FirstAsync();

    //    savedUser.PasswordResetTokenHash.Should().NotBe("old-hash");
    //}

    //[Fact]
    //public async Task Should_Set_Expiration_Correctly()
    //{
    //   var context = TestDbContextFactory.Create();

    //    var user = User.Create(
    //        "user@test.com",
    //        "John",
    //        "Doe",
    //        "hashedPassword",
    //        UserRole.Client);

    //    context.Users.Add(user);
    //    await context.SaveChangesAsync();

    //    var handler = new ForgotPasswordHandler(
    //        context,
    //        new FakeResetTokenGenerator());

    //    var before = DateTime.UtcNow;

    //    await handler.Handle(
    //        new ForgotPasswordCommand("user@test.com"),
    //        CancellationToken.None);

    //    var savedUser = await context.Users.FirstAsync();

    //    savedUser.PasswordResetTokenExpiresAtUtc.Should().NotBeNull();

    //    savedUser.PasswordResetTokenExpiresAtUtc!.Value
    //        .Should().BeAfter(before.AddMinutes(29));
    //}
}