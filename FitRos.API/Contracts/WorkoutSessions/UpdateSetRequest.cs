namespace FitRos.API.Contracts.WorkoutSessions;

public record UpdateSetRequest(int SetNumber, int RepsAchieved, decimal WeightUsed);
