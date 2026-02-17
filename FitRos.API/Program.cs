using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.WorkoutRoutines.AddExerciseToWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.ArchiveWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.CreateWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutineById;
using FitRos.Application.Features.WorkoutRoutines.GetWorkoutRoutines;
using FitRos.Application.Features.WorkoutRoutines.PublishWorkoutRoutine;
using FitRos.Application.Features.WorkoutRoutines.UpdateWorkoutRoutine;
using FitRos.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// =============================
// Services
// =============================

// Controllers
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// DbContext (PostgreSQL)
builder.Services.AddDbContext<FitRosDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection")
    )
);

// Register abstraction → implementation
builder.Services.AddScoped<IFitRosDbContext, FitRosDbContext>();

//  Register Handlers



//WorkoutRoutines 
builder.Services.AddScoped<CreateWorkoutRoutineHandler>();
builder.Services.AddScoped<GetWorkoutRoutinesHandler>();
builder.Services.AddScoped<GetWorkoutRoutineByIdHandler>();
builder.Services.AddScoped<ArchiveWorkoutRoutineHandler>();
builder.Services.AddScoped<UpdateWorkoutRoutineHandler>();
builder.Services.AddScoped<PublishWorkoutRoutineHandler>();
builder.Services.AddScoped<AddExerciseToWorkoutRoutineHandler>();




var app = builder.Build();

// =============================
// Middleware pipeline
// =============================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
