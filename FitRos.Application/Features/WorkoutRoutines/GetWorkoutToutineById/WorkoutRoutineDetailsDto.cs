using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;

public class WorkoutRoutineDetailsDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = null!;

    public string Description { get; init; } = null!;

    public int Version { get; init; }

    public int Status { get; init; }
}
