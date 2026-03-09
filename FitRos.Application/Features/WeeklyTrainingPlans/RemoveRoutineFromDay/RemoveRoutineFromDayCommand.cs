using MediatR;

namespace FitRos.Application.Features.WeeklyTrainingPlans.RemoveRoutineFromDay;

public record RemoveRoutineFromDayCommand(Guid PlanId, DayOfWeek Day) : IRequest;
