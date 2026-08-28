using MediatR;

namespace FitRos.Application.Features.MealPlans.MoveMealPlanEntry;

public record MoveMealPlanEntryCommand(
    Guid MealPlanId,
    Guid EntryId,
    int NewOrder
) : IRequest;
