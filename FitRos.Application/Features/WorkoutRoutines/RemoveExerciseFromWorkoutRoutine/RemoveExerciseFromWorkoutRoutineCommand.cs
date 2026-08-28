using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.RemoveExerciseFromWorkoutRoutine;

public record RemoveExerciseFromWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId
) : IRequest<bool>, ITenantGuardedRequest
{
    [JsonIgnore]
    public Guid ResourceId => WorkoutRoutineId;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
