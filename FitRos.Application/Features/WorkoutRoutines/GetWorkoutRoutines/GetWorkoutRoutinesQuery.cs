using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FitRos.Domain.Enums;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;

public class GetWorkoutRoutinesQuery
{
    // Optional filter by status (active, inactive)
    public RoutineStatus? Status { get; init; }
}
