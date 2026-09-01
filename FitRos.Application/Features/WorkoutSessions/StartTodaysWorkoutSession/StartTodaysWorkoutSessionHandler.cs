using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.WorkoutSessions;
using FitRos.Domain.Common;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutSessions.StartTodaysWorkoutSession;

public sealed class StartTodaysWorkoutSessionHandler
    : IRequestHandler<StartTodaysWorkoutSessionCommand, WorkoutSessionDetailsDto>
{
    private readonly IFitRosDbContext _context;
    private readonly ICurrentUser _currentUser;

    public StartTodaysWorkoutSessionHandler(IFitRosDbContext context, ICurrentUser currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<WorkoutSessionDetailsDto> Handle(
        StartTodaysWorkoutSessionCommand request,
        CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.UserId.HasValue)
            throw new UnauthorizedException("User not authenticated.");

        var clientProfile = await _context.ClientProfiles
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.UserId == _currentUser.UserId.Value, cancellationToken);

        if (clientProfile is null)
            throw new NotFoundException("You do not have a client profile yet.");

        // "Today" is resolved in UTC, matching the rest of the domain (CreatedAt, etc. are
        // all DateTime.UtcNow). If you train close to midnight local time this can pick the
        // wrong calendar day / day-of-week — revisit with proper timezone handling if that
        // turns out to matter in practice.
        var today = DateTime.UtcNow.Date;

        var existing = await _context.WorkoutSessions
            .IncludeSets()
            .FirstOrDefaultAsync(
                s => s.UserId == _currentUser.UserId.Value && s.ScheduledDate == today,
                cancellationToken);

        if (existing is not null && existing.Status != WorkoutSessionStatus.Skipped)
        {
            if (existing.Status == WorkoutSessionStatus.Scheduled)
            {
                existing.Start();
                await _context.SaveChangesAsync(cancellationToken);
            }

            var existingRoutine = await _context.WorkoutRoutines
                .AsNoTracking()
                .Include(r => r.Exercises)
                .FirstOrDefaultAsync(r => r.Id == existing.RoutineId, cancellationToken);

            return WorkoutSessionMapper.ToDetailsDto(existing, existingRoutine);
        }

        var routineId = request.RoutineId;

        if (!routineId.HasValue)
        {
            var activePlan = await _context.WeeklyTrainingPlans
                .AsNoTracking()
                .Include(p => p.Days)
                .FirstOrDefaultAsync(
                    p => p.ClientProfileId == clientProfile.Id && p.Status == TrainingPlanStatus.Active,
                    cancellationToken);

            var todayEntry = activePlan?.Days.FirstOrDefault(d => d.Day == today.DayOfWeek);

            if (todayEntry is null)
                throw new NotFoundException(
                    "No hay una rutina asignada para hoy. Elige una rutina manualmente para registrar tu entrenamiento.");

            routineId = todayEntry.WorkoutRoutineId;
        }

        var routine = await _context.WorkoutRoutines
            .AsNoTracking()
            .Include(r => r.Exercises)
            .FirstOrDefaultAsync(r => r.Id == routineId.Value, cancellationToken);

        if (routine is null)
            throw new NotFoundException("Workout routine not found.");

        var gymId = _currentUser.GymId ?? clientProfile.GymId
            ?? throw new ForbiddenException("Tenant context is missing.");

        var session = WorkoutSession.Create(
            gymId,
            _currentUser.UserId.Value,
            routine.Id,
            routine.Name,
            routine.Version,
            today);

        session.Start();

        _context.WorkoutSessions.Add(session);

        await _context.SaveChangesAsync(cancellationToken);

        return WorkoutSessionMapper.ToDetailsDto(session, routine);
    }
}
