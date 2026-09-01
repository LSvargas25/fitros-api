using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

public static class SqliteTestDbContextFactory
{
    public static FitRosDbContext Create(
        Guid? gymId = null,
        Guid? userId = null,
        UserRole role = UserRole.Admin)
    {
        gymId ??= Guid.NewGuid();
        userId ??= Guid.NewGuid();

        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = userId,
            Role = role,
            IsAuthenticated = true
        };

        var context = new FitRosDbContext(options, currentUser);

        context.Database.EnsureCreated();

        return context;
    }
}