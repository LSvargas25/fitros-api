using FitRos.API;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Behaviors;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using MediatR;
using Microsoft.AspNetCore.Hosting;
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
        builder.ConfigureTestServices(services =>
        {
            // Replace ICurrentUser with fake
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser>(_ =>
                new FakeCurrentUser(TestGymId)
                {
                    UserId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                    GymId = TestGymId,
                    Role = UserRole.Admin,
                    IsAuthenticated = true
                });

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