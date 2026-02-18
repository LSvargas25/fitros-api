using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FitRos.Domain.Enums;
namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;

public record CreateWorkoutRoutineCommand
{
    public string Name { get; init; } = null!;

    public string Description { get; init; } = null!;
}
