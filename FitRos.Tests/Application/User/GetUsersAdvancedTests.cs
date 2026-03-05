using FitRos.Application.Features.Users.GetUsersAdvanced;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Tests.Application.Users;

public class GetUsersAdvancedTests
{
    [Fact]
    public async Task Should_Paginate_With_Composite_Cursor_Without_Duplicates()
    {
        var gymId = Guid.NewGuid();

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.OwnerApp,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(currentUser);

        var baseDate = DateTime.UtcNow;

        // Create 3 users with SAME CreatedAt
        for (int i = 0; i < 3; i++)
        {
            var user = User.Create(
                $"user{i}@test.com",
                "Ana",
                "Test",
                "hash",
                UserRole.Client);

            user.AssignToGym(gymId);

            // Force same CreatedAt
            typeof(User).GetProperty(nameof(User.CreatedAt))!
                .SetValue(user, baseDate);

            context.Users.Add(user);
        }

        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetUsersAdvancedHandler(context, currentUser);

        // PAGE 1
        var page1 = await handler.Handle(
            new GetUsersAdvancedQuery(
                Cursor: null,
                PageSize: 2,
                SortBy: "createdAt",
                SortDirection: "desc",
                IncludeInactive: true
            ),
            CancellationToken.None);

        page1.Items.Should().HaveCount(2);
        page1.NextCursor.Should().NotBeNull();

        // PAGE 2
        var page2 = await handler.Handle(
            new GetUsersAdvancedQuery(
                Cursor: page1.NextCursor,
                PageSize: 2,
                SortBy: "createdAt",
                SortDirection: "desc",
                IncludeInactive: true
            ),
            CancellationToken.None);

        page2.Items.Should().HaveCount(1);

        var allIds = page1.Items.Select(x => x.Id)
            .Concat(page2.Items.Select(x => x.Id))
            .ToList();

        allIds.Distinct().Count().Should().Be(3);
    }
}