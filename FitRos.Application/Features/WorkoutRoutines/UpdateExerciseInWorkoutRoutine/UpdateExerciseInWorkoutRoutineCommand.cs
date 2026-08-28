using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateExerciseInWorkoutRoutine;

public record UpdateExerciseInWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId,
    int SuggestedSets,
    int SuggestedReps,
    int SuggestedRestSeconds
) : IRequest<bool>, ITenantGuardedRequest
{
    [JsonIgnore]
    public Guid ResourceId => WorkoutRoutineId;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
