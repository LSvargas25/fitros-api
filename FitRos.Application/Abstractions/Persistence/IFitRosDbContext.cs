using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FitRos.Application.Abstractions.Persistence;

public interface IFitRosDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<WorkoutRoutine> WorkoutRoutines { get; }
    IQueryable<WorkoutSession> WorkoutSessions { get; }

    IQueryable<Exercise> Exercises { get; }


    void AddWorkoutRoutine(WorkoutRoutine routine);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
