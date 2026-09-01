namespace FitRos.Application.Features.WorkoutSessions;

public sealed record ExerciseSetDto(
    Guid Id,
    Guid ExerciseId,
    int SetNumber,
    int RepsAchieved,
    decimal WeightUsed);
