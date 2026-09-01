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
using System.Threading;
using System.Threading.Tasks;

namespace FitRos.Application.Abstractions.Persistence;

public interface IFitRosDbContext
{
    DbSet<User> Users { get; }
    DbSet<WorkoutRoutine> WorkoutRoutines { get; }
    DbSet<WorkoutSession> WorkoutSessions { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Gym> Gyms { get; }

    DbSet<Exercise> Exercises { get; }

    DbSet<ClientProfile> ClientProfiles { get; }
    DbSet<PhysicalMeasure> PhysicalMeasures { get; }

    DbSet<AuditLogEntry> AuditLogEntries { get; }
    DbSet<ClientKpiSnapshot> ClientKpiSnapshots { get; }
    DbSet<OutboxMessage> OutboxMessages { get; }
    DbSet<ClientProgressReportSnapshot> ClientProgressReportSnapshots { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<WeeklyTrainingPlan> WeeklyTrainingPlans { get; }

    DbSet<Food> Foods { get; }
    DbSet<MealPlan> MealPlans { get; }
    DbSet<MealPlanEntry> MealPlanEntries { get; }

    void Remove<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}