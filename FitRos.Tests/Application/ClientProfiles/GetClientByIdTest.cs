using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.ClientProfiles.GetById;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace FitRos.Tests.Application.ClientProfiles.GetById
{ 
public sealed class GetClientByIdTest
{
    private static ICurrentUser MockUser(Guid? id, UserRole role)
    {
        var mock = new Mock<ICurrentUser>();
        mock.Setup(x => x.UserId).Returns(id);
        mock.Setup(x => x.Role).Returns(role);
        mock.Setup(x => x.IsAuthenticated).Returns(true);
        return mock.Object;
    }

    [Fact]
    public async Task Coach_should_get_own_client()
    {
        var coachId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var user = MockUser(coachId, UserRole.Coach);
        var context = TestDbContextFactory.Create(UserRole.Coach, coachId);

        var client = ClientProfile.Create(
            Guid.NewGuid(),
            coachId);

        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync();

        var handler = new GetClientByIdQueryHandler(context, user);

        var result = await handler.Handle(
            new GetClientByIdQuery(client.Id),
            CancellationToken.None);

        result.Id.Should().Be(client.Id);
    }

    [Fact]
    public async Task Coach_should_not_access_other_client()
    {
        var coachId = Guid.NewGuid();
        var otherCoachId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var user = MockUser(coachId, UserRole.Coach);
        var context = TestDbContextFactory.Create(UserRole.Coach, coachId);

        var client = ClientProfile.Create(
            Guid.NewGuid(),
            otherCoachId
            );

        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync();

        var handler = new GetClientByIdQueryHandler(context, user);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            handler.Handle(
                new GetClientByIdQuery(client.Id),
                CancellationToken.None));
    }

    [Fact]
    public async Task Admin_should_access_any_client()
    {
        var adminId = Guid.NewGuid();
        var createdBy = Guid.NewGuid();

        var user = MockUser(adminId, UserRole.Admin);
        var context = TestDbContextFactory.Create(UserRole.Admin, adminId);

        var client = ClientProfile.Create(
            Guid.NewGuid(),
            Guid.NewGuid());

        context.ClientProfiles.Add(client);
        await context.SaveChangesAsync();

        var handler = new GetClientByIdQueryHandler(context, user);

        var result = await handler.Handle(
            new GetClientByIdQuery(client.Id),
            CancellationToken.None);

        result.Id.Should().Be(client.Id);
    }

    [Fact]
    public async Task Should_throw_if_not_found()
    {
        var adminId = Guid.NewGuid();

        var user = MockUser(adminId, UserRole.Admin);
        var context = TestDbContextFactory.Create(UserRole.Admin, adminId);

        var handler = new GetClientByIdQueryHandler(context, user);

            await Assert.ThrowsAsync<NotFoundException>(() =>
                   handler.Handle(
                new GetClientByIdQuery(Guid.NewGuid()),
                CancellationToken.None));
    }
}

    
}
