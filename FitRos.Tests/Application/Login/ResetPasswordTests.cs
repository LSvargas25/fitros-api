using FitRos.Application.Features.Auth.ResetPassword;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Login;

public sealed class ResetPasswordTests
{
    [Fact]
    public async Task Should_Reset_Password_And_Clear_Token_When_Valid()
    {
        // Arrange
        var tokenGenerator = new FakeResetTokenGenerator();
        var passwordHasher = new FakePasswordHasher();

        var rawToken = "valid-token";
        var hashedToken = tokenGenerator.Hash(rawToken);

        var gymId = Guid.NewGuid();

        var context = TestDbContextFactory.Create(gymId);

        var user = User.CreateForGym(
            gymId,
            "user@test.com",
            "John",
            "Doe",
            "old-hash",
            UserRole.Client);

        user.SetPasswordResetToken(
            hashedToken,
            DateTime.UtcNow.AddMinutes(30));

        context.Users.Add(user);

        context.RefreshTokens.Add(
            RefreshToken.Create(
                user.Id,
                "refresh-hash",
                DateTime.UtcNow.AddDays(30)));

        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new ResetPasswordHandler(
            context,
            passwordHasher,
            tokenGenerator);

        // Act
        await handler.Handle(
            new ResetPasswordCommand(
                "user@test.com",
                rawToken,
                "NewPassword123"),
            CancellationToken.None);

        // Assert
        var expectedHash = passwordHasher.Hash("NewPassword123");

        var updatedUser = await context.Users
     .IgnoreQueryFilters()
     .FirstAsync(); ;

        updatedUser.PasswordHash.Should().Be(expectedHash);
        updatedUser.PasswordResetTokenHash.Should().BeNull();
        updatedUser.PasswordResetTokenExpiresAtUtc.Should().BeNull();

        context.RefreshTokens.Should().BeEmpty();
    }

    [Fact]
    public async Task Should_Throw_When_User_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var context = TestDbContextFactory.Create(gymId);

        var handler = new ResetPasswordHandler(
            context,
            new FakePasswordHasher(),
            new FakeResetTokenGenerator());

        var act = async () =>
            await handler.Handle(
                new ResetPasswordCommand(
                    "notfound@test.com",
                    "token",
                    "Password123"),
                CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid reset token.");
    }

    [Fact]
    public async Task Should_Throw_When_User_Is_Inactive()
    {
        var gymId = Guid.NewGuid();
        var context = TestDbContextFactory.Create(gymId);

        var tokenGenerator = new FakeResetTokenGenerator();

        var rawToken = "valid-token";
        var hashedToken = tokenGenerator.Hash(rawToken);

        var user = User.CreateForGym(
            gymId,
            "user@test.com",
            "John",
            "Doe",
            "old-hash",
            UserRole.Client);

        user.SetPasswordResetToken(hashedToken, DateTime.UtcNow.AddMinutes(30));
        user.Deactivate();

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new ResetPasswordHandler(
            context,
            new FakePasswordHasher(),
            tokenGenerator);

        var act = async () =>
            await handler.Handle(
                new ResetPasswordCommand(
                    "user@test.com",
                    rawToken,
                    "Password123"),
                CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid reset token.");
    }

    [Fact]
    public async Task Should_Throw_When_Token_Is_Invalid()
    {
        var gymId = Guid.NewGuid();
        var context = TestDbContextFactory.Create(gymId);

        var tokenGenerator = new FakeResetTokenGenerator();
        var validHash = tokenGenerator.Hash("valid-token");

        var user = User.CreateForGym(
            gymId,
            "user@test.com",
            "John",
            "Doe",
            "old-hash",
            UserRole.Client);

        user.SetPasswordResetToken(validHash, DateTime.UtcNow.AddMinutes(30));

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new ResetPasswordHandler(
            context,
            new FakePasswordHasher(),
            tokenGenerator);

        var act = async () =>
            await handler.Handle(
                new ResetPasswordCommand(
                    "user@test.com",
                    "wrong-token",
                    "Password123"),
                CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid reset token.");
    }

    [Fact]
    public async Task Should_Throw_When_Token_Is_Expired()
    {
        var gymId = Guid.NewGuid();
        var context = TestDbContextFactory.Create(gymId);

        var tokenGenerator = new FakeResetTokenGenerator();

        var rawToken = "valid-token";
        var hashedToken = tokenGenerator.Hash(rawToken);

        var user = User.CreateForGym(
            gymId,
            "user@test.com",
            "John",
            "Doe",
            "old-hash",
            UserRole.Client);

        user.SetPasswordResetToken(hashedToken, DateTime.UtcNow.AddMinutes(-5));

        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();

        var handler = new ResetPasswordHandler(
            context,
            new FakePasswordHasher(),
            tokenGenerator);

        var act = async () =>
            await handler.Handle(
                new ResetPasswordCommand(
                    "user@test.com",
                    rawToken,
                    "Password123"),
                CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid reset token.");
    }
}