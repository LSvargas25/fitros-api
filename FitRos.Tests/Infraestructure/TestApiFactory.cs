using FitRos.API;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Behaviors;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FitRos.Tests.Infrastructure;

public class TestApiFactory : WebApplicationFactory<Program>
{
    private static readonly Guid TestGymId =
        Guid.Parse("11111111-1111-1111-1111-111111111111");

    // Fixed name so ALL requests within a test share the same database
    private static readonly string DbName = "FitRosTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Don't let the auth rate limiter throttle test runs.
        builder.UseSetting("RateLimiting:Auth:PermitLimit", "100000");

        builder.ConfigureTestServices(services =>
        {
            // Replace ICurrentUser with a fake that stays consistent with the
            // authenticated principal: its Role tracks the request's role claim
            // (which TestAuthHandler sets from X-Test-Role, default Admin).
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser>(sp =>
            {
                var principal = sp.GetService<IHttpContextAccessor>()?.HttpContext?.User;
                var roleClaim = principal?.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
                var role = Enum.TryParse<UserRole>(roleClaim, out var parsed)
                    ? parsed
                    : TestAuthHandler.Role;

                return new FakeCurrentUser(TestGymId)
                {
                    UserId = TestAuthHandler.UserId,
                    GymId = TestGymId,
                    Role = role,
                    IsAuthenticated = true
                };
            });

            // Authenticate every request as that same principal so [Authorize]
            // controllers work in tests; a request carrying the
            // X-Test-Unauthenticated header is left anonymous (401).
            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            // Replace database with shared in-memory instance
            services.RemoveAll<DbContextOptions<FitRosDbContext>>();
            services.RemoveAll<IFitRosDbContext>();
            services.AddDbContext<FitRosDbContext>(options =>
                options.UseInMemoryDatabase(DbName)); // ← fixed name, not Guid.NewGuid()
            services.AddScoped<IFitRosDbContext>(sp =>
                sp.GetRequiredService<FitRosDbContext>());

            // Re-register behaviors
            services.RemoveAll(typeof(IPipelineBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthenticationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantGuardBehavior<,>));
        });
    }
}