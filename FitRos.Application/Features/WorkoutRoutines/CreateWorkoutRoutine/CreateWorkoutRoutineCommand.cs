using MediatR;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;

public record CreateWorkoutRoutineCommand : IRequest<CreateWorkoutRoutineResponse>
{
    public string Name { get; init; } = null!;

    // Optional. Nullable so [ApiController] does not implicitly require it at the
    // MVC edge (a non-nullable ref type is treated as [Required], which would
    // 400 "The Description field is required" and contradict the validator,
    // where Description only has a MaximumLength rule).
    public string? Description { get; init; }
}
