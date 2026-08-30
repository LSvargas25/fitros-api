using FitRos.Application.Common.Security;
using FitRos.Domain.Entities.Training;
using MediatR;
using System.Text.Json.Serialization;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;

public record UpdateWorkoutRoutineCommand(
    string Name,
    // Optional. Nullable so [ApiController] does not implicitly require it at the
    // MVC edge (a non-nullable ref type is treated as [Required], which would
    // 400 "The Description field is required" and contradict the validator,
    // where Description only has a MaximumLength rule).
    string? Description
) : IRequest<bool>, ITenantGuardedRequest
{
    public Guid Id { get; init; }

    [JsonIgnore]
    public Guid ResourceId => Id;
    [JsonIgnore]
    public Type EntityType => typeof(WorkoutRoutine);
}
