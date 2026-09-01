using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;

public class WorkoutRoutineDetailsDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = null!;
    public int Status { get; set; }
    public int Version { get; set; }

    public List<WorkoutRoutineExerciseDetailsDto> Exercises { get; set; } = new();
}
