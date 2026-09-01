using MediatR;

namespace FitRos.Application.Features.MealPlans.UpdateMealPlanEntry;

public record UpdateMealPlanEntryCommand(
    Guid MealPlanId,
    Guid EntryId,
    decimal QuantityGrams
) : IRequest;
