using FitRos.API.Middleware;
using FitRos.API.Swagger;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Features.Auth.Login;
using FitRos.Application.Features.Auth.Logout;
using FitRos.Application.Features.Auth.Refresh;
using FitRos.Application.Features.Exercises.ArchiveExercise;
using FitRos.Application.Features.Exercises.CreateExercise;
using FitRos.Application.Features.Exercises.GetExerciseById;
using FitRos.Application.Features.Exercises.GetExercises;
using FitRos.Application.Features.Exercises.UpdateExercise;
using FitRos.Application.Features.Users.ActivateUser;
using FitRos.Application.Features.Users.CreateUser;
using FitRos.Application.Features.Users.DeactivateUser;
using FitRos.Application.Features.Users.GetUserById;
using FitRos.Application.Features.Users.GetUsersAdvanced;
using FitRos.Application.Features.Users.UpdateUser;
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
using FitRos.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// =============================
// Controllers
// =============================

builder.Services.AddControllers();

// =============================
// FluentValidation
// =============================

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateWorkoutRoutineValidator>();

// =============================
// Swagger
// =============================

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
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

        return $"{httpOrder}_{api.RelativePath}";
    });

    // 🔐 JWT Bearer configuration for Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT Bearer token"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

builder.Services.AddFluentValidationRulesToSwagger();

// =============================
// Database (PostgreSQL)
// =============================

builder.Services.AddDbContext<FitRosDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

builder.Services.AddScoped<IFitRosDbContext, FitRosDbContext>();

// =============================
// JWT Configuration
// =============================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwtSection = builder.Configuration.GetSection("Jwt");

if (!jwtSection.Exists())
{
    throw new InvalidOperationException("Jwt configuration section is missing.");
}

var jwtSettings = jwtSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration is invalid."); ;

builder.Services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
builder.Services.AddScoped<ITokenService, TokenService>();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,

            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,

            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(
         Encoding.UTF8.GetBytes(jwtSettings.SigningKey)
     ),

            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromSeconds(30),

            NameClaimType = JwtRegisteredClaimNames.Sub,
            RoleClaimType = ClaimTypes.Role,

            ValidTypes = new[] { JwtConstants.TokenType }
        };
    });

builder.Services.AddAuthorization();

// =============================
// Current User (JWT-based)
// =============================

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

// =============================
// Handlers Registration
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

// Users
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<GetUserByIdHandler>();
builder.Services.AddScoped<GetUsersAdvancedHandler>();
builder.Services.AddScoped<DeactivateUserHandler>();
builder.Services.AddScoped<ActivateUserHandler>();
builder.Services.AddScoped<UpdateUserHandler>();
builder.Services.AddScoped<DeleteUserHandler>();

//Authentication
builder.Services.AddScoped<LoginHandler>();
builder.Services.AddScoped<RefreshHandler>();
builder.Services.AddScoped<LogoutHandler>();
 


// =============================
// Build App
// =============================

var app = builder.Build();

// =============================
// Middleware Pipeline
// =============================

app.UseMiddleware<GlobalExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();  
app.UseAuthorization();

app.MapControllers();

app.Run();