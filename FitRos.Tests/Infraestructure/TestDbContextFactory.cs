using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Common.Interceptors;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace FitRos.Tests.Infrastructure;

public sealed class TestDbContextContainer
{
    public FitRosDbContext Context { get; }
    public FakeCurrentUser CurrentUser { get; }

    public TestDbContextContainer(FitRosDbContext context, FakeCurrentUser currentUser)
    {
        Context = context;
        CurrentUser = currentUser;
    }
}

public static class TestDbContextFactory
{
    public static TestDbContextContainer CreateContainer(UserRole role = UserRole.Admin)
    {
        var gymId = Guid.NewGuid();
        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = role,
            IsAuthenticated = true
        };

        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var context = new FitRosDbContext(options, currentUser);
        return new TestDbContextContainer(context, currentUser);
    }

    public static FitRosDbContext Create()
    {
        return CreateContainer().Context;
    }

    public static FitRosDbContext Create(UserRole role)
    {
        return CreateContainer(role).Context;
    }

    public static FitRosDbContext Create(Guid gymId)
    {
        var currentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new FitRosDbContext(options, currentUser);
    }

    public static FitRosDbContext Create(ICurrentUser currentUser)
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        return new FitRosDbContext(options, currentUser);
    }

    
    public static FitRosDbContext CreateWithInterceptor(ICurrentUser currentUser)
    {
        var interceptor = new OutboxInterceptor();

        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .AddInterceptors(interceptor)
            .Options;

        return new FitRosDbContext(options, currentUser);
    }
}