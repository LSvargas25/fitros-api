using FitRos.Application.Features.MealPlans.AddMealPlanEntry;
using FitRos.Application.Features.MealPlans.GetMealPlanById;
using FitRos.Application.Features.MealPlans.RemoveMealPlanEntry;
using FitRos.Application.Features.MealPlans.UpdateMealPlanEntry;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FitRos.Tests.Application.MealPlans;

public class MealPlanEntryTests
{
    private static FakeCurrentUser Admin(Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = UserRole.Admin,
        IsAuthenticated = true
    };

    private static async Task<(MealPlan plan, Food food)> SeedPlanAndFood(
        FitRosDbContext context, Guid gymId)
    {
        var plan = MealPlan.Create(Guid.NewGuid(), Guid.NewGuid(), gymId, "Plan");
        var food = Food.Create("Chicken", FoodCategory.Protein, 165m, 31m, 0m, 3.6m, null, gymId);
        context.MealPlans.Add(plan);
        context.Foods.Add(food);
        await context.SaveChangesAsync(CancellationToken.None);
        return (plan, food);
    }

    [Fact]
    public async Task Add_Should_Throw_When_Plan_Not_Found()
    {
        var user = Admin(Guid.NewGuid());
        var context = TestDbContextFactory.Create(user);
        var handler = new AddMealPlanEntryHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AddMealPlanEntryCommand(Guid.NewGuid(), DayOfWeek.Monday, MealType.Breakfast, Guid.NewGuid(), 100m),
                CancellationToken.None));
    }

    [Fact]
    public async Task Add_Should_Throw_When_Food_Not_Found()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);
        var (plan, _) = await SeedPlanAndFood(context, gymId);

        var handler = new AddMealPlanEntryHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(
                new AddMealPlanEntryCommand(plan.Id, DayOfWeek.Monday, MealType.Lunch, Guid.NewGuid(), 100m),
                CancellationToken.None));
    }

    [Fact]
    public async Task Add_Should_Insert_Entry()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);
        var (plan, food) = await SeedPlanAndFood(context, gymId);

        var handler = new AddMealPlanEntryHandler(context, user);
        await handler.Handle(
            new AddMealPlanEntryCommand(plan.Id, DayOfWeek.Monday, MealType.Lunch, food.Id, 200m),
            CancellationToken.None);

        var saved = await context.MealPlans.Include(p => p.Entries).SingleAsync(p => p.Id == plan.Id);
        saved.Entries.Should().ContainSingle();
        saved.Entries.Single().QuantityGrams.Should().Be(200m);
    }

    [Fact]
    public async Task Add_Same_Food_In_Same_Slot_Replaces_Quantity()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);
        var (plan, food) = await SeedPlanAndFood(context, gymId);

        var handler = new AddMealPlanEntryHandler(context, user);
        var cmd1 = new AddMealPlanEntryCommand(plan.Id, DayOfWeek.Monday, MealType.Lunch, food.Id, 200m);
        await handler.Handle(cmd1, CancellationToken.None);
        await handler.Handle(cmd1 with { QuantityGrams = 250m }, CancellationToken.None);

        var saved = await context.MealPlans.Include(p => p.Entries).SingleAsync(p => p.Id == plan.Id);
        saved.Entries.Should().ContainSingle();
        saved.Entries.Single().QuantityGrams.Should().Be(250m);
    }

    [Fact]
    public async Task Update_Then_Remove_Entry()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);
        var (plan, food) = await SeedPlanAndFood(context, gymId);

        var add = new AddMealPlanEntryHandler(context, user);
        await add.Handle(
            new AddMealPlanEntryCommand(plan.Id, DayOfWeek.Tuesday, MealType.Dinner, food.Id, 150m),
            CancellationToken.None);

        var entryId = (await context.MealPlans.Include(p => p.Entries).SingleAsync(p => p.Id == plan.Id))
            .Entries.Single().Id;

        var update = new UpdateMealPlanEntryHandler(context, user);
        await update.Handle(new UpdateMealPlanEntryCommand(plan.Id, entryId, 175m), CancellationToken.None);

        (await context.MealPlanEntries.SingleAsync(e => e.Id == entryId)).QuantityGrams.Should().Be(175m);

        var remove = new RemoveMealPlanEntryHandler(context, user);
        await remove.Handle(new RemoveMealPlanEntryCommand(plan.Id, entryId), CancellationToken.None);

        (await context.MealPlans.Include(p => p.Entries).SingleAsync(p => p.Id == plan.Id))
            .Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_Returns_Entries_With_Computed_Macros_And_Totals()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);
        var (plan, food) = await SeedPlanAndFood(context, gymId); // Chicken: 165 kcal / 31 P / 0 C / 3.6 F per 100 g

        var add = new AddMealPlanEntryHandler(context, user);
        await add.Handle(
            new AddMealPlanEntryCommand(plan.Id, DayOfWeek.Monday, MealType.Lunch, food.Id, 200m),
            CancellationToken.None);

        var handler = new GetMealPlanByIdHandler(context, user);
        var dto = await handler.Handle(new GetMealPlanByIdQuery(plan.Id), CancellationToken.None);

        dto.Entries.Should().ContainSingle();
        var entry = dto.Entries.Single();
        entry.FoodName.Should().Be("Chicken");
        entry.Calories.Should().Be(330m);   // 165 * 2
        entry.Protein.Should().Be(62m);     // 31 * 2
        entry.Fat.Should().Be(7.2m);        // 3.6 * 2

        dto.TotalCalories.Should().Be(330m);
        dto.TotalProtein.Should().Be(62m);
    }
}
