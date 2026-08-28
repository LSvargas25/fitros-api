using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Nutrition;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Domain.Nutrition;

public class MealPlanEntryOrderingTests
{
    private static MealPlan NewPlan() =>
        MealPlan.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Plan");

    private static (Guid a, Guid b, Guid c) AddThreeToMondayLunch(MealPlan plan)
    {
        plan.AddEntry(DayOfWeek.Monday, MealType.Lunch, Guid.NewGuid(), 100m);
        plan.AddEntry(DayOfWeek.Monday, MealType.Lunch, Guid.NewGuid(), 100m);
        plan.AddEntry(DayOfWeek.Monday, MealType.Lunch, Guid.NewGuid(), 100m);
        var slot = plan.Entries
            .Where(e => e is { Day: DayOfWeek.Monday, Meal: MealType.Lunch })
            .OrderBy(e => e.Order)
            .ToList();
        return (slot[0].Id, slot[1].Id, slot[2].Id);
    }

    [Fact]
    public void AddEntry_numbers_new_foods_1_to_N_within_their_slot()
    {
        var plan = NewPlan();

        var (a, b, c) = AddThreeToMondayLunch(plan);

        plan.Entries.Single(e => e.Id == a).Order.Should().Be(1);
        plan.Entries.Single(e => e.Id == b).Order.Should().Be(2);
        plan.Entries.Single(e => e.Id == c).Order.Should().Be(3);
    }

    [Fact]
    public void AddEntry_orders_are_independent_per_slot()
    {
        var plan = NewPlan();

        plan.AddEntry(DayOfWeek.Monday, MealType.Lunch, Guid.NewGuid(), 100m);
        plan.AddEntry(DayOfWeek.Tuesday, MealType.Dinner, Guid.NewGuid(), 100m);

        plan.Entries.Should().OnlyContain(e => e.Order == 1);
    }

    [Fact]
    public void MoveEntry_to_the_front_renumbers_the_slot()
    {
        var plan = NewPlan();
        var (a, b, c) = AddThreeToMondayLunch(plan);

        plan.MoveEntry(c, 1);

        plan.Entries.Single(e => e.Id == c).Order.Should().Be(1);
        plan.Entries.Single(e => e.Id == a).Order.Should().Be(2);
        plan.Entries.Single(e => e.Id == b).Order.Should().Be(3);
    }

    [Fact]
    public void MoveEntry_to_the_middle_renumbers_the_slot()
    {
        var plan = NewPlan();
        var (a, b, c) = AddThreeToMondayLunch(plan);

        plan.MoveEntry(a, 2);

        plan.Entries.Single(e => e.Id == b).Order.Should().Be(1);
        plan.Entries.Single(e => e.Id == a).Order.Should().Be(2);
        plan.Entries.Single(e => e.Id == c).Order.Should().Be(3);
    }

    [Fact]
    public void MoveEntry_does_not_touch_other_slots()
    {
        var plan = NewPlan();
        var (a, _, _) = AddThreeToMondayLunch(plan);
        plan.AddEntry(DayOfWeek.Tuesday, MealType.Dinner, Guid.NewGuid(), 100m);
        var tuesdayId = plan.Entries.Single(e => e.Day == DayOfWeek.Tuesday).Id;

        plan.MoveEntry(a, 3);

        plan.Entries.Single(e => e.Id == tuesdayId).Order.Should().Be(1);
    }

    [Fact]
    public void MoveEntry_is_a_noop_when_the_position_is_unchanged()
    {
        var plan = NewPlan();
        var (a, b, c) = AddThreeToMondayLunch(plan);

        plan.MoveEntry(b, 2);

        plan.Entries.Single(e => e.Id == a).Order.Should().Be(1);
        plan.Entries.Single(e => e.Id == b).Order.Should().Be(2);
        plan.Entries.Single(e => e.Id == c).Order.Should().Be(3);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(4)]
    [InlineData(-1)]
    public void MoveEntry_rejects_a_position_outside_the_slot(int newOrder)
    {
        var plan = NewPlan();
        var (a, _, _) = AddThreeToMondayLunch(plan);

        var act = () => plan.MoveEntry(a, newOrder);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void MoveEntry_throws_NotFound_for_an_unknown_entry()
    {
        var plan = NewPlan();
        AddThreeToMondayLunch(plan);

        var act = () => plan.MoveEntry(Guid.NewGuid(), 1);

        act.Should().Throw<NotFoundException>();
    }

    [Fact]
    public void MoveEntry_is_blocked_on_an_archived_plan()
    {
        var plan = NewPlan();
        var (a, _, _) = AddThreeToMondayLunch(plan);
        plan.Archive();

        var act = () => plan.MoveEntry(a, 1);

        act.Should().Throw<DomainException>();
    }
}
