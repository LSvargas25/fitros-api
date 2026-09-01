using FitRos.Application.Features.WorkoutRoutines;
using FitRos.Domain.Entities.Training;

namespace FitRos.Application.Features.WorkoutSessions;

public static class WorkoutSessionMapper
{
    public static WorkoutSessionDetailsDto ToDetailsDto(WorkoutSession session, WorkoutRoutine? routine)
    {
        var suggested = routine?.Exercises
            .OrderBy(e => e.Order)
            .Select(e => new WorkoutRoutineExerciseDto(
                e.ExerciseId,
                e.Order,
                e.SuggestedSets,
                e.SuggestedReps,
                e.SuggestedRestSeconds))
            .ToList() ?? new List<WorkoutRoutineExerciseDto>();

        var sets = session.Sets
            .OrderBy(s => s.SetNumber)
            .Select(s => new ExerciseSetDto(s.Id, s.ExerciseId, s.SetNumber, s.RepsAchieved, s.WeightUsed))
            .ToList();

        return new WorkoutSessionDetailsDto(
            session.Id,
            session.RoutineId,
            session.RoutineNameSnapshot,
            session.RoutineVersion,
            session.ScheduledDate,
            session.Status,
            sets,
            suggested);
    }
}
