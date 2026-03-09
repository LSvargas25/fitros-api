namespace FitRos.API.Contracts.WeeklyTrainingPlans;

public record AssignRoutineToDayRequest(Guid WorkoutRoutineId, string? Notes);
