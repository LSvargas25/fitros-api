using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;

public record GetWorkoutRoutineByIdQuery(Guid Id)
    : IRequest<WorkoutRoutineDetailsDto?>, ITenantGuardedRequest
{
    [JsonIgnore]
    public Guid ResourceId => Id;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
