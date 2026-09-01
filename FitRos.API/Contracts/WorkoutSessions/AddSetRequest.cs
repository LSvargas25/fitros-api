namespace FitRos.API.Contracts.WorkoutSessions;

public record AddSetRequest(Guid ExerciseId, int SetNumber, int RepsAchieved, decimal WeightUsed);
