using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;

public record PublishWorkoutRoutineCommand(Guid Id)
    : IRequest, ITenantGuardedRequest
{
    [JsonIgnore]
    public Guid ResourceId => Id;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
