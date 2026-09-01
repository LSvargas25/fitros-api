using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;

public class WorkoutRoutineListItem
{
    public Guid Id { get; init; }
    public string Name { get; init; } = null!;
    public int Version { get; init; }
    public string Status { get; init; } = null!;
}
