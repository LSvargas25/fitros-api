using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineVersions;

public sealed record GetWorkoutRoutineVersionsQuery(
    Guid WorkoutRoutineId
) : IRequest<List<WorkoutRoutineVersionListItem>>, ITenantGuardedRequest
{
    [JsonIgnore]
    public Guid ResourceId => WorkoutRoutineId;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
