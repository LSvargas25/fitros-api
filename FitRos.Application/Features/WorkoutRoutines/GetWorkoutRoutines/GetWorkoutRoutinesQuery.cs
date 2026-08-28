using FitRos.Application.Common.Models;
using FitRos.Domain.Enums;
using MediatR;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;

// No ITenantGuardedRequest: this is a collection query with no single resource
// id. Tenant scoping comes from the global GymId query filter on WorkoutRoutines.
public record GetWorkoutRoutinesQuery(
    RoutineStatus? Status,
    int Page = 1,
    int PageSize = 10
) : IRequest<PagedResult<WorkoutRoutineListItem>>;
