using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.WorkoutRoutines.GetWorkoutToutineById
{
    public class WorkoutRoutineExerciseDetailsDto
    {
        public Guid ExerciseId { get; set; }
        public int Order { get; set; }
        public int SuggestedSets { get; set; }
        public int SuggestedReps { get; set; }
        public int SuggestedRestSeconds { get; set; }
    }
}
