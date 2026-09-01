using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;

public record UpdateWorkoutRoutineCommand(
    string Name,
    string Description
);
