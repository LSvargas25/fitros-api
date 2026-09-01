using FitRos.Application.Features.Foods.CreateFood;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Moq;
using Xunit;

namespace FitRos.Tests.Application.Foods;

public class CreateFoodTests
{
    private static CreateFoodCommand ValidCommand(string name = "Chicken Breast") =>
        new(name, FoodCategory.Protein, 165m, 31m, 0m, 3.6m, 150m);

    [Fact]
    public async Task Should_Throw_When_Not_Authenticated()
    {
        var currentUser = new Mock<FitRos.Application.Abstractions.Security.ICurrentUser>();
        currentUser.Setup(x => x.IsAuthenticated).Returns(false);

        var context = TestDbContextFactory.Create(currentUser.Object);
        var handler = new CreateFoodHandler(context, currentUser.Object);

        await Assert.ThrowsAsync<UnauthorizedException>(() =>
            handler.Handle(ValidCommand(), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Food_Name_Already_Exists()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        context.Foods.Add(Food.Create("Chicken Breast", FoodCategory.Protein, 165m, 31m, 0m, 3.6m, null, gymId));
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new CreateFoodHandler(context, fakeUser);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(ValidCommand("chicken breast"), CancellationToken.None));
    }

    [Fact]
    public async Task Should_Create_Food_When_Authenticated()
    {
        var gymId = Guid.NewGuid();
        var fakeUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new CreateFoodHandler(context, fakeUser);

        var result = await handler.Handle(ValidCommand(), CancellationToken.None);

        result.Id.Should().NotBeEmpty();
        var food = context.Foods.Single(f => f.Id == result.Id);
        food.Name.Should().Be("Chicken Breast");
        food.NormalizedName.Should().Be("chicken breast");
        food.ProteinPer100g.Should().Be(31m);
        food.GymId.Should().Be(gymId);
    }

    [Fact]
    public async Task Should_Throw_When_Macros_Negative()
    {
        var fakeUser = new FakeCurrentUser(Guid.NewGuid())
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var context = TestDbContextFactory.Create(fakeUser);
        var handler = new CreateFoodHandler(context, fakeUser);

        var command = ValidCommand() with { ProteinPer100g = -1m };

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(command, CancellationToken.None));
    }
}
