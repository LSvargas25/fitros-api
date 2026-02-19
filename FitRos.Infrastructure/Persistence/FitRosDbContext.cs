using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using FitRos.Application.Abstractions.Persistence;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Infrastructure.Persistence;

public class FitRosDbContext : DbContext, IFitRosDbContext
{
    public FitRosDbContext(DbContextOptions<FitRosDbContext> options)
        : base(options)
    {
    }

    // =========================
    // EF DbSets (REAL TABLES)
    // =========================

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<WorkoutRoutine> WorkoutRoutines { get; set; } = null!;
    public DbSet<WorkoutSession> WorkoutSessions { get; set; } = null!;
    public DbSet<Exercise> Exercises { get; set; } = null!; 

    // =========================
    // Interface IQueryable
    // =========================

    IQueryable<User> IFitRosDbContext.Users => Users;
    IQueryable<WorkoutRoutine> IFitRosDbContext.WorkoutRoutines => WorkoutRoutines;
    IQueryable<WorkoutSession> IFitRosDbContext.WorkoutSessions => WorkoutSessions;
    IQueryable<Exercise> IFitRosDbContext.Exercises => Exercises;

    // =========================
    // Commands
    // =========================

    public void AddExercise(Exercise exercise)
    {
        Exercises.Add(exercise); 
    }

    public void AddWorkoutRoutine(WorkoutRoutine routine)
    {
        WorkoutRoutines.Add(routine);
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(FitRosDbContext).Assembly);

        // =========================================
        // WorkoutRoutine → Backing Field Mapping
        // =========================================
        modelBuilder.Entity<WorkoutRoutine>(builder =>
        {
            builder.HasKey(r => r.Id);

            builder.HasMany(typeof(WorkoutRoutineExercise), "_exercises")
                   .WithOne()
                   .HasForeignKey("WorkoutRoutineId");

            builder.Navigation("_exercises")
                   .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        base.OnModelCreating(modelBuilder);
    }

}
