using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace FitRos.Infrastructure.Persistence;

public class FitRosDbContext : DbContext, IFitRosDbContext
{
    public FitRosDbContext(DbContextOptions<FitRosDbContext> options)
        : base(options)
    {
    }

    // =========================
    // DbSets
    // =========================

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<WorkoutRoutine> WorkoutRoutines { get; set; } = null!;
    public DbSet<WorkoutSession> WorkoutSessions { get; set; } = null!;
    public DbSet<Exercise> Exercises { get; set; } = null!;
    public DbSet<WorkoutRoutineExercise> WorkoutRoutineExercises { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public DbSet<ClientProfile> ClientProfiles { get; set; } = null!;
    public DbSet<PhysicalMeasure> PhysicalMeasures { get; set; } = null!;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        => base.SaveChangesAsync(cancellationToken);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FitRosDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    public void Remove<TEntity>(TEntity entity) where TEntity : class
    {
        Set<TEntity>().Remove(entity);
    }
}