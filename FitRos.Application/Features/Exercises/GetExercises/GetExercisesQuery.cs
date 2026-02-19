using FitRos.Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitRos.Application.Features.Exercises.GetExercises;

public record GetExercisesQuery(
    MuscleGroup? Category
);