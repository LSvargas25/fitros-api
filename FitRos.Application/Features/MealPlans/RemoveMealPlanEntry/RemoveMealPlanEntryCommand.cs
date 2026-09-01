using MediatR;

namespace FitRos.Application.Features.MealPlans.RemoveMealPlanEntry;

public record RemoveMealPlanEntryCommand(
    Guid MealPlanId,
    Guid EntryId
) : IRequest;
