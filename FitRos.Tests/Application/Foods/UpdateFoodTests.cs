using FitRos.Application.Features.Foods.UpdateFood;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Foods;

public class UpdateFoodTests
{
    private static FakeCurrentUser Admin(Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = UserRole.Admin,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Should_Throw_When_Food_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);
        var handler = new UpdateFoodHandler(context, user);

        var command = new UpdateFoodCommand(
            Guid.NewGuid(), "X", FoodCategory.Protein, 100m, 10m, 10m, 10m, null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(command, CancellationToken.None));
    }

    [Fact]
    public async Task Should_Update_Food_Fields()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var food = Food.Create("Rice", FoodCategory.Carbohydrate, 130m, 2.7m, 28m, 0.3m, null, gymId);
        context.Foods.Add(food);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateFoodHandler(context, user);

        await handler.Handle(
            new UpdateFoodCommand(food.Id, "White Rice", FoodCategory.Carbohydrate, 135m, 2.8m, 29m, 0.4m, 200m),
            CancellationToken.None);

        var updated = context.Foods.Single(f => f.Id == food.Id);
        updated.Name.Should().Be("White Rice");
        updated.CaloriesPer100g.Should().Be(135m);
        updated.ServingSizeGrams.Should().Be(200m);
    }

    [Fact]
    public async Task Should_Throw_When_Renaming_To_An_Existing_Food_Name()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        context.Foods.Add(Food.Create("Rice", FoodCategory.Carbohydrate, 130m, 2.7m, 28m, 0.3m, null, gymId));
        var oats = Food.Create("Oats", FoodCategory.Carbohydrate, 389m, 17m, 66m, 7m, null, gymId);
        context.Foods.Add(oats);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateFoodHandler(context, user);

        await Assert.ThrowsAsync<DomainException>(() =>
            handler.Handle(
                new UpdateFoodCommand(oats.Id, "Rice", FoodCategory.Carbohydrate, 389m, 17m, 66m, 7m, null),
                CancellationToken.None));
    }

    [Fact]
    public async Task Should_Throw_When_Food_Is_Archived()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var food = Food.Create("Rice", FoodCategory.Carbohydrate, 130m, 2.7m, 28m, 0.3m, null, gymId);
        food.Archive();
        context.Foods.Add(food);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new UpdateFoodHandler(context, user);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(
                new UpdateFoodCommand(food.Id, "Rice", FoodCategory.Carbohydrate, 130m, 2.7m, 28m, 0.3m, null),
                CancellationToken.None));
    }
}
