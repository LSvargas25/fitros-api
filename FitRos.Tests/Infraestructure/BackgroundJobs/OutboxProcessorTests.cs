using FitRos.Application.Abstractions.Security;
using FitRos.Infrastructure.Common.BackgroundJobs;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace FitRos.Tests.Infrastructure.BackgroundJobs;

public class OutboxProcessorTests
{
    // A database that can't be queried (here: no schema, so every query
    // throws) stands in for Postgres being down or still waking up. Since
    // .NET 6 an exception escaping a BackgroundService stops the whole host,
    // so the processor must survive it and try again on the next tick.
    [Fact]
    public async Task A_database_failure_does_not_escape_and_stop_the_host()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var services = new ServiceCollection();
        services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser(null));
        services.AddDbContext<FitRosDbContext>(o => o.UseSqlite(connection));
        services.AddSingleton(Mock.Of<IPublisher>());
        await using var provider = services.BuildServiceProvider();

        var processor = new OutboxProcessor(
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<OutboxProcessor>.Instance);

        await processor.StartAsync(CancellationToken.None);
        await Task.Delay(300);

        processor.ExecuteTask.Should().NotBeNull();
        processor.ExecuteTask!.IsFaulted.Should().BeFalse("a failed poll must be logged and retried, not crash the host");

        await processor.StopAsync(CancellationToken.None);
    }
}
