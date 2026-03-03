using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Moq;

namespace FitRos.Tests.Infrastructure;

public static class TestDbContextFactory
{
    public static FitRosDbContext Create(
        UserRole role = UserRole.Admin,
        Guid? userId = null,
        bool isAuthenticated = true)
    {
        var options = new DbContextOptionsBuilder<FitRosDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w =>
                w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var mockUser = new Mock<ICurrentUser>();

        mockUser.Setup(x => x.UserId)
            .Returns(userId ?? Guid.NewGuid());

        mockUser.Setup(x => x.Role)
            .Returns(role);

        mockUser.Setup(x => x.IsAuthenticated)
            .Returns(isAuthenticated);

        return new FitRosDbContext(options, mockUser.Object);
    }
}