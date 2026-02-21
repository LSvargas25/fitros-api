using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
using FitRos.Domain.Entities.Users;

namespace FitRos.Application.Abstractions.Persistence;

public interface IFitRosDbContext
{
    DbSet<User> Users { get; }
    DbSet<WorkoutRoutine> WorkoutRoutines { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<Exercise> Exercises { get; }

    void Remove<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
