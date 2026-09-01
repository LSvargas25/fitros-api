using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Auth.GoogleLogin;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.Auth;

public class GoogleLoginHandlerTests
{
    private static GoogleLoginHandler CreateHandler(
        FitRos.Infrastructure.Persistence.FitRosDbContext context,
        GoogleUserInfo? googleUser)
        => new(
            context,
            new FakeGoogleTokenValidator(googleUser),
            new FakePasswordHasher(),
            new FakeResetTokenGenerator(),
            new FakeTokenService());

    [Fact]
    public async Task Creates_Independent_Verified_Client_For_New_Google_User()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var handler = CreateHandler(
            context, new GoogleUserInfo("new.google@test.com", true, "Ana", "Lopez"));

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        var user = await context.Users.IgnoreQueryFilters().FirstAsync(u => u.Id == result.UserId);
        var profile = await context.ClientProfiles.IgnoreQueryFilters().FirstAsync(p => p.UserId == user.Id);

        user.Role.Should().Be(UserRole.Client);
        user.EmailVerified.Should().BeTrue();
        user.GymId.Should().BeNull();
        profile.GymId.Should().BeNull();
        profile.CoachId.Should().BeNull();
        result.AccessToken.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Logs_In_Existing_User_By_Email()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));

        var existing = User.Create("existing@test.com", "A", "B", "hash", UserRole.Client);
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var handler = CreateHandler(
            context, new GoogleUserInfo("existing@test.com", true, "A", "B"));

        var result = await handler.Handle(new GoogleLoginCommand("valid-token"), CancellationToken.None);

        result.UserId.Should().Be(existing.Id);

        var usersCount = await context.Users.IgnoreQueryFilters()
            .CountAsync(u => u.NormalizedEmail == "EXISTING@TEST.COM");
        usersCount.Should().Be(1);
    }

    [Fact]
    public async Task Throws_When_Token_Is_Invalid()
    {
        var context = TestDbContextFactory.Create(new FakeCurrentUser(null));
        var handler = CreateHandler(context, null);

        Func<Task> act = async () =>
            await handler.Handle(new GoogleLoginCommand("bad-token"), CancellationToken.None);

        await act.Should().ThrowAsync<DomainException>();
    }
}
