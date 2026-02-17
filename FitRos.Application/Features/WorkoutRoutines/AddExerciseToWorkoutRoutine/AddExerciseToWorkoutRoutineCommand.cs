using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;

public record AddExerciseToWorkoutRoutineCommand(
    Guid WorkoutRoutineId,
    Guid ExerciseId,
    int Order,
    int SuggestedSets,
    int SuggestedReps,
    int SuggestedRestSeconds
);
