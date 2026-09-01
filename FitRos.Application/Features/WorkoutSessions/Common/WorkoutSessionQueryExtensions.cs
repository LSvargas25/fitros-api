using FitRos.Domain.Entities.Training;
using Microsoft.EntityFrameworkCore;

namespace FitRos.Application.Features.WorkoutSessions;

public static class WorkoutSessionQueryExtensions
{
    // WorkoutSession.Sets is mapped as a private shadow navigation ("_sets", see
    // WorkoutSessionConfiguration.HasMany(typeof(ExerciseSet), "_sets")), and the public
    // Sets property is explicitly ignored by the EF mapping. That means
    // `Include(x => x.Sets)` does not work here (the expression-based overload requires a
    // mapped navigation) — the shadow navigation has to be included by its EF-model name.
    public static IQueryable<WorkoutSession> IncludeSets(this IQueryable<WorkoutSession> query)
        => query.Include("_sets");
}
