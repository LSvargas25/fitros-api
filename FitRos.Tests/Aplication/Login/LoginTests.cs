using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Auth.Login;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Tests.Application.Auth;

public class LoginTests
{
    private static FitRosDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new FitRosDbContext(options);
    }

    [Fact]
    public async Task Should_Login_Successfully_With_Valid_Credentials()
    {
        // Arrange
        var context = CreateDbContext();

        IPasswordHasher hasher = new TestPasswordHasher();
        ITokenService tokens = new FakeTokenService();

        var password = "Password123";

        var user = User.Create(
            email: "test@test.com",
            firstName: "Test",
            lastName: "User",
            passwordHash: hasher.Hash(password),
            role: UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var handler = new LoginHandler(context, hasher, tokens);

        // Act
        var result = await handler.Handle(
            new LoginCommand("test@test.com", password),
            CancellationToken.None);

        // Assert
        result.AccessToken.Should().NotBeNullOrEmpty();
        result.RefreshToken.Should().NotBeNullOrEmpty();
        result.Email.Should().Be("test@test.com");
        result.Role.Should().Be((int)UserRole.Client);

        var storedRefresh = await context.RefreshTokens.FirstOrDefaultAsync();
        storedRefresh.Should().NotBeNull();
        storedRefresh!.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task Should_Throw_When_Password_Is_Invalid()
    {
        var context = CreateDbContext();

        IPasswordHasher hasher = new TestPasswordHasher();
        ITokenService tokens = new FakeTokenService();

        var user = User.Create(
            "test@test.com",
            "Test",
            "User",
            hasher.Hash("CorrectPassword"),
            UserRole.Client);

        context.Add(user);
        await context.SaveChangesAsync();

        var handler = new LoginHandler(context, hasher, tokens);

        var act = async () =>
            await handler.Handle(new LoginCommand("test@test.com", "WrongPassword"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Invalid credentials.");
    }

    private sealed class TestPasswordHasher : IPasswordHasher
    {
        // Deterministic and reversible-enough for tests only.
        public string Hash(string password) => $"TEST_HASH::{password}";

        public bool Verify(string password, string passwordHash) =>
            passwordHash == $"TEST_HASH::{password}";
    }
}