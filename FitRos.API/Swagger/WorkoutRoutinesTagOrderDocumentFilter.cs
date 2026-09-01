using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace FitRos.API.Swagger;

public sealed class WorkoutRoutinesTagOrderDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        swaggerDoc.Tags = new List<OpenApiTag>
        {
            new() { Name = "WorkoutRoutines - Core" },
            new() { Name = "WorkoutRoutines - Versioning" },
            new() { Name = "WorkoutRoutines - Lifecycle" },
            new() { Name = "WorkoutRoutines - Exercises" }
        };
    }
}