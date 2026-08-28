using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;

public record AddExerciseToWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId,
    int Order,
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
