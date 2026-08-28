using MediatR;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;

public record CreateWorkoutRoutineCommand : IRequest<CreateWorkoutRoutineResponse>
{
    public string Name { get; init; } = null!;

    public string Description { get; init; } = null!;
}
