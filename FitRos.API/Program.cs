using FitRos.API.Middleware;
using FitRos.API.Swagger;
using FitRos.Application;
using FitRos.Application.Abstractions.Messaging;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Behaviors;
using FitRos.Application.Common.Behaviors;
using FitRos.Infrastructure.Common.BackgroundJobs;
using FitRos.Infrastructure.Common.Interceptors;
using FitRos.Infrastructure.Messaging;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Infrastructure.Security;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using MicroElements.Swashbuckle.FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
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
// CORS
// =============================
// Allowed origins come from Cors:AllowedOrigins (array). Local dev gets
// http://localhost:4200 from appsettings.Development.json; on Render the value
// is supplied via Cors__AllowedOrigins__0 (see render.yaml). No origins
// configured means no cross-origin browser calls are allowed.

var corsOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularDev", policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// =============================
// FluentValidation
// =============================

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<AssemblyReference>();

// =============================
// MediatR + Pipeline Behaviors
// =============================

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(AssemblyReference).Assembly));

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(AuthenticationBehavior<,>));

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(AuthorizationBehavior<,>));

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehavior<,>));

builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(TenantGuardBehavior<,>));

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

builder.Services.AddSingleton<OutboxInterceptor>();

builder.Services.AddDbContext<FitRosDbContext>((sp, options) =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .AddInterceptors(sp.GetRequiredService<OutboxInterceptor>()));

builder.Services.AddScoped<IFitRosDbContext, FitRosDbContext>();

// =============================
// JWT Configuration
// =============================

builder.Services.Configure<JwtSettings>(
    builder.Configuration.GetSection("Jwt"));

var jwtSection = builder.Configuration.GetSection("Jwt");

if (!jwtSection.Exists())
    throw new InvalidOperationException("Jwt configuration section is missing.");

var jwtSettings = jwtSection.Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt configuration is invalid.");

builder.Services.AddScoped<IPasswordHasher, PasswordHasherAdapter>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IPasswordResetTokenGenerator, PasswordResetTokenGenerator>();
builder.Services.AddScoped<IEmailVerificationCodeGenerator, EmailVerificationCodeGenerator>();
builder.Services.AddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection("Authentication:Google"));

builder.Services.Configure<FrontendSettings>(
    builder.Configuration.GetSection("Frontend"));

builder.Services.Configure<SmtpSettings>(
    builder.Configuration.GetSection("Smtp"));

builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();

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
// WorkoutRoutine Handlers (direct, no MediatR)
// =============================

var applicationAssembly = typeof(AssemblyReference).Assembly;

var handlerTypes = applicationAssembly
    .GetTypes()
    .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Handler")
             && !typeof(IPipelineBehavior<,>).IsAssignableFrom(t));

foreach (var handlerType in handlerTypes)
{
    builder.Services.AddScoped(handlerType);
}

builder.Services.AddHostedService<OutboxProcessor>();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // The "Testing" environment (WebApplicationFactory-based auth tests)
    // fires far more than 5 requests at these endpoints per run from the
    // same loopback connection, so it gets an effectively unlimited window
    // instead of disabling the policy outright.
    var authPermitLimit = builder.Environment.IsEnvironment("Testing") ? int.MaxValue : 5;

    options.AddFixedWindowLimiter("auth", limiterOptions =>
    {
        limiterOptions.PermitLimit = authPermitLimit;
        limiterOptions.Window = TimeSpan.FromMinutes(5);
        limiterOptions.QueueLimit = 0;
    });
});

var app = builder.Build();

// Lets a deploy set its own owner password without committing a hash to
// source: if Auth__OwnerSeedPasswordHash is set, apply it to the seeded
// OwnerApp account once at boot instead of relying on the placeholder
// baked into the migrations.
var ownerSeedPasswordHash = app.Configuration["Auth:OwnerSeedPasswordHash"];
if (!string.IsNullOrWhiteSpace(ownerSeedPasswordHash))
{
    using var seedScope = app.Services.CreateScope();
    var db = seedScope.ServiceProvider.GetRequiredService<FitRosDbContext>();
    var owner = await db.Users
        .IgnoreQueryFilters()
        .FirstOrDefaultAsync(u => u.Role == UserRole.OwnerApp);

    if (owner is not null && owner.PasswordHash != ownerSeedPasswordHash)
    {
        owner.ChangePasswordHash(ownerSeedPasswordHash);
        await db.SaveChangesAsync();
    }
}

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

app.UseCors("AllowAngularDev");

app.UseAuthentication();
app.UseAuthorization();

app.UseRateLimiter();

app.MapControllers();

// Unauthenticated liveness probe for the platform health check.
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();

public partial class Program { }