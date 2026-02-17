using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Training;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;

namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;

public class CreateWorkoutRoutineHandler
{
    private readonly IFitRosDbContext _context;

    public CreateWorkoutRoutineHandler(IFitRosDbContext context)
    {
        _context = context;
    }

    public async Task<CreateWorkoutRoutineResponse> Handle(
        CreateWorkoutRoutineCommand command,
        CancellationToken cancellationToken)
    {
        var routine = WorkoutRoutine.Create(
            command.Name,
            command.Description
        );

        _context.AddWorkoutRoutine(routine);

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateWorkoutRoutineResponse(
            routine.Id,
            routine.Name,
            routine.Version
        );
    }
}
