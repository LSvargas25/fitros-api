using FitRos.Application.Features.Auth.Register;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Auth;

public class RegisterHandlerTests
{
    [Fact]
    public async Task Creates_Unverified_Independent_Client()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var handler = new RegisterHandler(
            context,
            new FakePasswordHasher(),
            new FakeEmailVerificationCodeGenerator(),
            new FakeEmailSender());

        var result = await handler.Handle(
            new RegisterCommand("new.client@test.com", "New", "Client", "Password123!"),
            CancellationToken.None);

        var user = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == result.UserId);
        var profile = await context.ClientProfiles.IgnoreQueryFilters().FirstAsync(p => p.UserId == result.UserId);

        user.Role.Should().Be(UserRole.Client);
        user.GymId.Should().BeNull();
        user.EmailVerified.Should().BeFalse();
        user.EmailVerificationCodeHash.Should().NotBeNullOrEmpty();

        profile.GymId.Should().BeNull();
        profile.CoachId.Should().BeNull();
    }

    [Fact]
    public async Task Throws_When_Email_Already_Registered()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));

        var existing = User.Create("taken@test.com", "A", "B", "hash", UserRole.Client);
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var handler = new RegisterHandler(
            context,
            new FakePasswordHasher(),
            new FakeEmailVerificationCodeGenerator(),
            new FakeEmailSender());

        Func<Task> act = async () =>
            await handler.Handle(
                new RegisterCommand("taken@test.com", "New", "Client", "Password123!"),
                CancellationToken.None);

        await act.Should()
            .ThrowAsync<DomainException>()
            .WithMessage("Email already exists.");
    }
}
