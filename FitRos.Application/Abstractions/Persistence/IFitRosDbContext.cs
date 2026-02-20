using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace FitRos.Application.Abstractions.Persistence;

public interface IFitRosDbContext
{
    DbSet<User> Users { get; }
    DbSet<WorkoutRoutine> WorkoutRoutines { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<Exercise> Exercises { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
