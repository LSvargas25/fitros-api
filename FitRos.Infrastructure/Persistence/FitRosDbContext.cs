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

                    // EF Core DbSet properties (kept for EF)
                    public DbSet<User> Users { get; set; } = null!;
                    public DbSet<WorkoutRoutine> WorkoutRoutines { get; set; } = null!;
                    public DbSet<WorkoutSession> WorkoutSessions { get; set; } = null!;

    public IQueryable<Exercise> Exercises => throw new NotImplementedException();

    // Explicit interface implementations to match IFitRosDbContext (IQueryable<T>)
    IQueryable<User> IFitRosDbContext.Users => Users;
                    IQueryable<WorkoutRoutine> IFitRosDbContext.WorkoutRoutines => WorkoutRoutines;
                    IQueryable<WorkoutSession> IFitRosDbContext.WorkoutSessions => WorkoutSessions;

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

                        base.OnModelCreating(modelBuilder);
                    }
                }
