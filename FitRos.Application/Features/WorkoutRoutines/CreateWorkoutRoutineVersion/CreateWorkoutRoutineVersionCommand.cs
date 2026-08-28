using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;

public sealed record CreateWorkoutRoutineVersionCommand(
    Guid WorkoutRoutineId
) : IRequest<CreateWorkoutRoutineVersionResponse>, ITenantGuardedRequest
{
    [JsonIgnore]
    public Guid ResourceId => WorkoutRoutineId;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
