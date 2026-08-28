using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;

public record UpdateWorkoutRoutineCommand(
    string Name,
    string Description
) : IRequest<bool>, ITenantGuardedRequest
{
    public Guid Id { get; init; }

    [JsonIgnore]
    public Guid ResourceId => Id;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
