using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FitRos.Domain.Enums;
namespace FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;

public record CreateWorkoutRoutineCommand(
    string Name,
    string Description
);
