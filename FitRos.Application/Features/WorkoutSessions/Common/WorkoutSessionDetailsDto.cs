using FitRos.Application.Features.WorkoutRoutines;
using FitRos.Domain.Enums;

namespace FitRos.Application.Features.WorkoutSessions;

public sealed record WorkoutSessionDetailsDto(
    Guid Id,
    Guid RoutineId,
    string RoutineNameSnapshot,
    int RoutineVersion,
    DateTime ScheduledDate,
    WorkoutSessionStatus Status,
    List<ExerciseSetDto> Sets,
    List<WorkoutRoutineExerciseDto> SuggestedExercises);
