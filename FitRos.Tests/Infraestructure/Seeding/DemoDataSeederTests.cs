using FitRos.Application.Features.Auth.Login;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Infrastructure.Persistence.Seeding;
using FitRos.Infrastructure.Security;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Tests.Infrastructure.Seeding;

// Runs against SQLite (relational, real constraints and the HasData owner
// seed) and opens a fresh DbContext per "boot", the way Program.cs does.
public sealed class DemoDataSeederTests : IDisposable
{
    private static readonly string[] AdvertisedLogins =
    {
        DemoDataSeeder.OwnerEmail,
        DemoDataSeeder.AdminEmail,
        DemoDataSeeder.CoachEmail,
        DemoDataSeeder.ClientEmail,
    };

    private readonly SqliteConnection _connection;
    private readonly PasswordHasherAdapter _hasher = new();

    public DemoDataSeederTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        using var db = NewContext();
        db.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    // Same shape as a boot-time scope: no HTTP user, so no gym in context.
    private FitRosDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new FitRosDbContext(options, new FakeCurrentUser(null));
    }

    private async Task<DemoSeedResult> RunSeederAsync()
    {
        await using var db = NewContext();
        return await new DemoDataSeeder(db, _hasher).SeedAsync();
    }

    private async Task<Dictionary<string, int>> CountRowsAsync()
    {
        await using var db = NewContext();

        return new Dictionary<string, int>
        {
            ["Gyms"] = await db.Gyms.IgnoreQueryFilters().CountAsync(),
            ["Users"] = await db.Users.IgnoreQueryFilters().CountAsync(),
            ["ClientProfiles"] = await db.ClientProfiles.IgnoreQueryFilters().CountAsync(),
            ["PhysicalMeasures"] = await db.PhysicalMeasures.CountAsync(),
            ["Exercises"] = await db.Exercises.IgnoreQueryFilters().CountAsync(),
            ["WorkoutRoutines"] = await db.WorkoutRoutines.IgnoreQueryFilters().CountAsync(),
            ["WorkoutRoutineExercises"] = await db.WorkoutRoutineExercises.CountAsync(),
            ["WeeklyTrainingPlans"] = await db.WeeklyTrainingPlans.IgnoreQueryFilters().CountAsync(),
            ["TrainingPlanDays"] = await db.TrainingPlanDays.CountAsync(),
            ["Foods"] = await db.Foods.IgnoreQueryFilters().CountAsync(),
            ["MealPlans"] = await db.MealPlans.IgnoreQueryFilters().CountAsync(),
            ["MealPlanEntries"] = await db.MealPlanEntries.CountAsync(),
            ["WorkoutSessions"] = await db.WorkoutSessions.IgnoreQueryFilters().CountAsync(),
            ["Notifications"] = await db.Notifications.IgnoreQueryFilters().CountAsync(),
        };
    }

    [Fact]
    public async Task Running_the_seeder_twice_does_not_duplicate_anything()
    {
        var first = await RunSeederAsync();
        var afterFirst = await CountRowsAsync();

        var second = await RunSeederAsync();
        var afterSecond = await CountRowsAsync();

        first.GymCreated.Should().BeTrue();
        first.UsersCreated.Should().Be(DemoDataSeeder.AllEmails.Count);

        second.Should().Be(new DemoSeedResult(GymCreated: false, UsersCreated: 0, UsersRepaired: 0));
        afterSecond.Should().Equal(afterFirst);

        // The first run must actually have produced rows in every table it owns.
        afterFirst.Should().OnlyContain(kv => kv.Value > 0);
    }

    [Fact]
    public async Task Running_it_three_times_still_leaves_one_demo_gym_and_one_user_per_email()
    {
        await RunSeederAsync();
        await RunSeederAsync();
        await RunSeederAsync();

        await using var db = NewContext();

        (await db.Gyms.IgnoreQueryFilters().CountAsync(g => g.Id == DemoDataSeeder.DemoGymId))
            .Should().Be(1);

        var emails = await db.Users.IgnoreQueryFilters()
            .Where(u => u.Email.EndsWith("@fitros.demo"))
            .Select(u => u.Email)
            .ToListAsync();

        emails.Should().OnlyHaveUniqueItems()
            .And.BeEquivalentTo(DemoDataSeeder.AllEmails);
    }

    [Fact]
    public async Task Seeds_one_active_verified_login_per_role_with_the_demo_password()
    {
        await RunSeederAsync();
        await using var db = NewContext();

        var expected = new Dictionary<string, UserRole>
        {
            [DemoDataSeeder.OwnerEmail] = UserRole.OwnerApp,
            [DemoDataSeeder.AdminEmail] = UserRole.Admin,
            [DemoDataSeeder.CoachEmail] = UserRole.Coach,
            [DemoDataSeeder.ClientEmail] = UserRole.Client,
        };

        foreach (var (email, role) in expected)
        {
            var user = await db.Users.IgnoreQueryFilters()
                .SingleAsync(u => u.NormalizedEmail == email.ToUpperInvariant());

            user.Role.Should().Be(role);
            user.Status.Should().Be(UserStatus.Active);
            user.EmailVerified.Should().BeTrue();
            _hasher.Verify(DemoDataSeeder.DemoPassword, user.PasswordHash).Should().BeTrue();

            if (role == UserRole.OwnerApp)
                user.GymId.Should().BeNull();
            else
                user.GymId.Should().Be(DemoDataSeeder.DemoGymId);
        }
    }

    [Fact]
    public async Task Demo_logins_work_through_the_real_login_handler()
    {
        await RunSeederAsync();

        foreach (var email in AdvertisedLogins)
        {
            await using var db = NewContext();
            var handler = new LoginHandler(db, _hasher, new FakeTokenService());

            var response = await handler.Handle(
                new LoginCommand(email, DemoDataSeeder.DemoPassword),
                CancellationToken.None);

            response.Email.Should().Be(email);
            response.AccessToken.Should().NotBeNullOrEmpty();
        }
    }

    [Fact]
    public async Task Every_role_lands_on_screens_with_data()
    {
        await RunSeederAsync();
        await using var db = NewContext();

        var users = await db.Users.IgnoreQueryFilters()
            .Where(u => u.Email.EndsWith("@fitros.demo"))
            .ToDictionaryAsync(u => u.Email);

        var coach = users[DemoDataSeeder.CoachEmail];
        var client = users[DemoDataSeeder.ClientEmail];

        // Coach: a roster.
        var coachClients = await db.ClientProfiles.IgnoreQueryFilters()
            .Where(p => p.CoachId == coach.Id)
            .ToListAsync();
        coachClients.Should().HaveCountGreaterThanOrEqualTo(3);

        // Client: an active weekly plan covering every day, an active meal
        // plan, a measurement history and completed sessions.
        var profile = coachClients.Single(p => p.UserId == client.Id);

        var plan = await db.WeeklyTrainingPlans.IgnoreQueryFilters()
            .Include(p => p.Days)
            .SingleAsync(p => p.ClientProfileId == profile.Id && p.Status == TrainingPlanStatus.Active);
        plan.Days.Select(d => d.Day).Should().BeEquivalentTo(Enum.GetValues<DayOfWeek>());

        (await db.MealPlans.IgnoreQueryFilters()
            .AnyAsync(m => m.ClientProfileId == profile.Id && m.Status == MealPlanStatus.Active))
            .Should().BeTrue();

        var measuredAt = await db.PhysicalMeasures
            .Where(m => m.ClientProfileId == profile.Id)
            .Select(m => m.RecordedAt)
            .ToListAsync();
        measuredAt.Should().HaveCountGreaterThanOrEqualTo(4);
        measuredAt.Distinct().Should().HaveCount(measuredAt.Count, "the history must span several dates");
        measuredAt.Should().OnlyContain(d => d < DateTime.UtcNow);

        (await db.WorkoutSessions.IgnoreQueryFilters()
            .CountAsync(s => s.UserId == client.Id && s.Status == WorkoutSessionStatus.Completed))
            .Should().BeGreaterThan(5);

        // Routines: published ones to assign, and a draft to edit.
        var routineStatuses = await db.WorkoutRoutines.IgnoreQueryFilters()
            .Where(r => r.GymId == DemoDataSeeder.DemoGymId)
            .Select(r => r.Status)
            .ToListAsync();
        routineStatuses.Should().Contain(RoutineStatus.Published).And.Contain(RoutineStatus.Draft);

        // Every advertised login has at least one unread notification.
        foreach (var email in AdvertisedLogins)
        {
            var id = users[email].Id;
            (await db.Notifications.IgnoreQueryFilters().AnyAsync(n => n.UserId == id && !n.IsRead))
                .Should().BeTrue($"{email} should have an unread notification");
        }
    }

    [Fact]
    public async Task Rerun_repairs_a_demo_user_a_visitor_deactivated_and_changed_the_password_of()
    {
        await RunSeederAsync();
        var before = await CountRowsAsync();

        await using (var db = NewContext())
        {
            var coach = await db.Users.IgnoreQueryFilters()
                .SingleAsync(u => u.NormalizedEmail == DemoDataSeeder.CoachEmail.ToUpperInvariant());
            coach.ChangePasswordHash(_hasher.Hash("SomethingElse1!"));
            coach.Deactivate();
            await db.SaveChangesAsync();
        }

        var result = await RunSeederAsync();

        result.Should().Be(new DemoSeedResult(GymCreated: false, UsersCreated: 0, UsersRepaired: 1));
        (await CountRowsAsync()).Should().Equal(before);

        await using var check = NewContext();
        var repaired = await check.Users.IgnoreQueryFilters()
            .SingleAsync(u => u.NormalizedEmail == DemoDataSeeder.CoachEmail.ToUpperInvariant());
        repaired.Status.Should().Be(UserStatus.Active);
        _hasher.Verify(DemoDataSeeder.DemoPassword, repaired.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Does_not_touch_the_migration_seeded_platform_owner()
    {
        string hashBefore;
        await using (var db = NewContext())
        {
            var owner = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == FitRosDbContext.SeedOwnerId);
            owner.Email.Should().Be("owner@fitros.com");
            hashBefore = owner.PasswordHash;
        }

        await RunSeederAsync();
        await RunSeederAsync();

        await using var after = NewContext();
        (await after.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == FitRosDbContext.SeedOwnerId))
            .PasswordHash.Should().Be(hashBefore);
    }
}
