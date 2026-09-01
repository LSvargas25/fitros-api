using FitRos.Application.Abstractions.Persistence;
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

namespace FitRos.Tests.Application.ClientProfiles
{
    public sealed class GetMyClientsTests
    {
        private sealed class TestFitRosDbContext : DbContext, IFitRosDbContext
        {
            public TestFitRosDbContext(DbContextOptions<TestFitRosDbContext> options) : base(options) { }

            public DbSet<User> Users { get; set; } = null!;
            public DbSet<Gym> Gyms => Set<Gym>();

            public DbSet<ClientProfile> ClientProfiles { get; set; } = null!;
            public DbSet<PhysicalMeasure> PhysicalMeasures { get; set; } = null!;

            public DbSet<AuditLogEntry> AuditLogEntries { get; set; } = null!;
            public DbSet<ClientKpiSnapshot> ClientKpiSnapshots { get; set; } = null!;
            public DbSet<OutboxMessage> OutboxMessages { get; set; } = null!;
            public DbSet<ClientProgressReportSnapshot> ClientProgressReportSnapshots { get; set; } = null!;

            public DbSet<Notification> Notifications => Set<Notification>();

            public DbSet<WeeklyTrainingPlan> WeeklyTrainingPlans => Set<WeeklyTrainingPlan>();

            public DbSet<WorkoutRoutine> WorkoutRoutines => Set<WorkoutRoutine>();
            public DbSet<WorkoutSession> WorkoutSessions => Set<WorkoutSession>();
            public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
            public DbSet<Exercise> Exercises => Set<Exercise>();

            public DbSet<Food> Foods => Set<Food>();
            public DbSet<MealPlan> MealPlans => Set<MealPlan>();
            public DbSet<MealPlanEntry> MealPlanEntries => Set<MealPlanEntry>();

            public new void Remove<TEntity>(TEntity entity) where TEntity : class => Set<TEntity>().Remove(entity);

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                modelBuilder.Entity<User>().HasKey(x => x.Id);

                modelBuilder.Entity<Gym>().HasKey(x => x.Id);

                modelBuilder.Entity<ClientProfile>().HasKey(x => x.Id);

                modelBuilder.Entity<PhysicalMeasure>().HasKey(x => x.Id);

                modelBuilder.Entity<ClientProfile>()
                    .HasMany(x => x.Measures)
                    .WithOne()
                    .HasForeignKey(x => x.ClientProfileId);

                modelBuilder.Entity<AuditLogEntry>().HasKey(x => x.Id);
                modelBuilder.Entity<ClientKpiSnapshot>().HasKey(x => x.Id);
                modelBuilder.Entity<OutboxMessage>().HasKey(x => x.Id);
                modelBuilder.Entity<ClientProgressReportSnapshot>().HasKey(x => x.Id);

                base.OnModelCreating(modelBuilder);
            }
        }
    }
}