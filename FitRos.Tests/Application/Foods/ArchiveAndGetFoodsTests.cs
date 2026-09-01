using FitRos.Application.Features.Foods.ArchiveFood;
using FitRos.Application.Features.Foods.GetFoods;
using FitRos.Domain.Common;
using FitRos.Domain.Enums;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Application.Foods;

public class ArchiveAndGetFoodsTests
{
    private static FakeCurrentUser Admin(Guid gymId) => new(gymId)
    {
        UserId = Guid.NewGuid(),
        Role = UserRole.Admin,
        IsAuthenticated = true
    };

    [Fact]
    public async Task Archive_Should_Throw_When_Food_Not_Found()
    {
        var user = Admin(Guid.NewGuid());
        var context = TestDbContextFactory.Create(user);
        var handler = new ArchiveFoodHandler(context, user);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            handler.Handle(new ArchiveFoodCommand(Guid.NewGuid()), CancellationToken.None));
    }

    [Fact]
    public async Task Archive_Should_Mark_Food_Archived()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var food = Food.Create("Rice", FoodCategory.Carbohydrate, 130m, 2.7m, 28m, 0.3m, null, gymId);
        context.Foods.Add(food);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new ArchiveFoodHandler(context, user);
        await handler.Handle(new ArchiveFoodCommand(food.Id), CancellationToken.None);

        context.Foods.Single(f => f.Id == food.Id).IsArchived.Should().BeTrue();
    }

    [Fact]
    public async Task GetFoods_Should_Exclude_Archived_And_Filter_By_Category()
    {
        var gymId = Guid.NewGuid();
        var user = Admin(gymId);
        var context = TestDbContextFactory.Create(user);

        var chicken = Food.Create("Chicken", FoodCategory.Protein, 165m, 31m, 0m, 3.6m, null, gymId);
        var rice = Food.Create("Rice", FoodCategory.Carbohydrate, 130m, 2.7m, 28m, 0.3m, null, gymId);
        var archivedProtein = Food.Create("Old Protein", FoodCategory.Protein, 100m, 20m, 0m, 1m, null, gymId);
        archivedProtein.Archive();
        context.Foods.AddRange(chicken, rice, archivedProtein);
        await context.SaveChangesAsync(CancellationToken.None);

        var handler = new GetFoodsHandler(context);

        var proteins = await handler.Handle(new GetFoodsQuery(FoodCategory.Protein), CancellationToken.None);
        proteins.Should().ContainSingle().Which.Name.Should().Be("Chicken");

        var all = await handler.Handle(new GetFoodsQuery(null), CancellationToken.None);
        all.Select(f => f.Name).Should().BeEquivalentTo(new[] { "Chicken", "Rice" });

        var withArchived = await handler.Handle(new GetFoodsQuery(null, IncludeArchived: true), CancellationToken.None);
        withArchived.Should().HaveCount(3);
    }
}
