using FitRos.API.Middleware;
using FitRos.API.Swagger;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.Exercises.ArchiveExercise;
using FitRos.Application.Features.Exercises.CreateExercise;
using FitRos.Application.Features.Exercises.GetExerciseById;
using FitRos.Application.Features.Exercises.GetExercises;
using FitRos.Application.Features.Exercises.UpdateExercise;
using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.ArchiveWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutineVersion;
using FitRos.Application.Features.WorkoutRoutines.GetLatestWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineVersions;
using FitRos.Application.Features.WorkoutRoutines.MoveExerciseInWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.RemoveExerciseFromWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Infrastructure.Persistence;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =============================
// Services
// =============================

// Controllers
builder.Services.AddControllers();

// FluentValidation (auto-validation for [ApiController] model binding)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateWorkoutRoutineValidator>();

// Swagger + FluentValidation rules
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FitRos.API",
        Version = "v1",
        Description = "API for FitRos Gym Management Platform"
    });

    options.EnableAnnotations();

    options.DocumentFilter<WorkoutRoutinesTagOrderDocumentFilter>();

    options.OrderActionsBy(api =>
    {
        var httpOrder = api.HttpMethod switch
        {
            "GET" => "1",
            "POST" => "2",
            "PUT" => "3",
            "DELETE" => "4",
            _ => "9"
        };

        var relativePath = api.RelativePath ?? string.Empty;
        var httpMethod = api.HttpMethod ?? string.Empty;

        return $"{httpOrder}_{httpMethod}_{relativePath}";
    });
});
builder.Services.AddFluentValidationRulesToSwagger();

// DbContext (PostgreSQL)
builder.Services.AddDbContext<FitRosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Register abstraction → implementation
builder.Services.AddScoped<IFitRosDbContext, FitRosDbContext>();

// =============================
// Handlers
// =============================

// WorkoutRoutines
builder.Services.AddScoped<CreateWorkoutRoutineHandler>();
builder.Services.AddScoped<GetWorkoutRoutinesHandler>();
builder.Services.AddScoped<GetWorkoutRoutineByIdHandler>();
builder.Services.AddScoped<ArchiveWorkoutRoutineHandler>();
builder.Services.AddScoped<UpdateWorkoutRoutineHandler>();
builder.Services.AddScoped<PublishWorkoutRoutineHandler>();
builder.Services.AddScoped<AddExerciseToWorkoutRoutineHandler>();
builder.Services.AddScoped<RemoveExerciseFromWorkoutRoutineHandler>();
builder.Services.AddScoped<MoveExerciseInWorkoutRoutineHandler>();
builder.Services.AddScoped<CreateWorkoutRoutineVersionHandler>();
builder.Services.AddScoped<GetWorkoutRoutineVersionsHandler>();
builder.Services.AddScoped<GetLatestWorkoutRoutineHandler>();

// Exercises
builder.Services.AddScoped<CreateExerciseHandler>();
builder.Services.AddScoped<GetExerciseByIdHandler>();
builder.Services.AddScoped<GetExercisesHandler>();
builder.Services.AddScoped<UpdateExerciseHandler>();
builder.Services.AddScoped<ArchiveExerciseHandler>();

var app = builder.Build();

// =============================
// Middleware pipeline
// =============================

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FitRos.API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();