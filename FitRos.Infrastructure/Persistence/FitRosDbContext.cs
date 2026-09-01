using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Analytics;
using FitRos.Domain.Entities.Auditing;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Gym;
using FitRos.Domain.Entities.Notifications;
using FitRos.Domain.Entities.Nutrition;
using FitRos.Domain.Entities.Outbox;
using FitRos.Domain.Entities.Reports;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Entities.WeeklyTraining;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Infrastructure.Persistence;

public class FitRosDbContext : DbContext, IFitRosDbContext
{
    private readonly ICurrentUser _currentUser;

    public FitRosDbContext(
       DbContextOptions<FitRosDbContext> options,
       ICurrentUser currentUser)
       : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<WorkoutRoutine> WorkoutRoutines { get; set; } = null!;
    public DbSet<WorkoutSession> WorkoutSessions { get; set; } = null!;
    public DbSet<Gym> Gyms { get; set; } = null!;
    public DbSet<Exercise> Exercises { get; set; } = null!;
    public DbSet<WorkoutRoutineExercise> WorkoutRoutineExercises { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;

    public Guid? CurrentGymId => _currentUser.GymId;

    public DbSet<ClientProfile> ClientProfiles { get; set; } = null!;
    public DbSet<PhysicalMeasure> PhysicalMeasures { get; set; } = null!;
    public DbSet<AuditLogEntry> AuditLogEntries { get; set; } = null!;
    public DbSet<ClientKpiSnapshot> ClientKpiSnapshots { get; set; } = null!;
    public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
    public DbSet<ClientProgressReportSnapshot> ClientProgressReportSnapshots { get; set; } = null!;

    public DbSet<Notification> Notifications { get; set; } = null!;

    public DbSet<WeeklyTrainingPlan> WeeklyTrainingPlans { get; set; } = null!;
    public DbSet<TrainingPlanDay> TrainingPlanDays { get; set; } = null!;

    public DbSet<Food> Foods { get; set; } = null!;
    public DbSet<MealPlan> MealPlans { get; set; } = null!;
    public DbSet<MealPlanEntry> MealPlanEntries { get; set; } = null!;

    DbSet<AuditLogEntry> IFitRosDbContext.AuditLogEntries => AuditLogEntries;
    DbSet<ClientKpiSnapshot> IFitRosDbContext.ClientKpiSnapshots => ClientKpiSnapshots;
    DbSet<OutboxMessage> IFitRosDbContext.OutboxMessages => OutboxMessages;
    DbSet<ClientProgressReportSnapshot> IFitRosDbContext.ClientProgressReportSnapshots => ClientProgressReportSnapshots;

    public override async Task<int> SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUser?.UserId;
        var gymId = _currentUser?.GymId;

        foreach (var entry in ChangeTracker.Entries())
        {
            // =========================
            // AUDIT
            // =========================

            if (entry.Entity is IAuditableEntity auditable)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Property(nameof(IAuditableEntity.CreatedAt))
                         .CurrentValue = DateTime.UtcNow;

                    if (userId.HasValue)
                        entry.Property(nameof(IAuditableEntity.CreatedBy))
                             .CurrentValue = userId.Value;
                }

                if (entry.State == EntityState.Modified)
                {
                    if (userId.HasValue)
                        auditable.SetModified(userId.Value);
                }
            }

            // =========================
            // TENANT AUTO ASSIGN
            // =========================

            if (entry.Entity is ITenantEntity tenantEntity)
            {
                if (entry.State == EntityState.Added)
                {
                    if (tenantEntity.GymId == null && gymId.HasValue)
                    {
                        tenantEntity.GymId = gymId.Value;
                    }
                }
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FitRosDbContext).Assembly);

        modelBuilder.Entity<ClientProfile>()
            .HasQueryFilter(c => c.Status != ClientStatus.Deleted);

        if (Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL")
        {
            modelBuilder.Entity<ClientProfile>()
                .UseXminAsConcurrencyToken();
        }

        modelBuilder.ApplyTenantQueryFilters(this);

        SeedOwner(modelBuilder);

        base.OnModelCreating(modelBuilder);
    }

    public new void Remove<TEntity>(TEntity entity) where TEntity : class
    {
        Set<TEntity>().Remove(entity);
    }
    private static void SeedOwner(ModelBuilder modelBuilder)
    {
        var ownerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

        var owner = User.CreateOwnerApp(
            ownerId,
            "owner@fitros.com",
            "FitRos",
            "Owner",
            "AQAAAAIAAYagAAAAELe0J5jOZGpfuPmwQO01ca1V7Q7UBF6m/sJkq/Z8GrxFQYv6RxjKnlqfGpwRwMjWAQ==",
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        );

        modelBuilder.Entity<User>().HasData(owner);
    }
}