using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;

public record MoveExerciseInWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId,
    int NewOrder
) : IRequest<bool>, ITenantGuardedRequest
{
    [JsonIgnore]
    public Guid ResourceId => WorkoutRoutineId;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
