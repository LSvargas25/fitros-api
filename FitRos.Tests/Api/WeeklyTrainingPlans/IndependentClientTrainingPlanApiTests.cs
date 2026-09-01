using FitRos.API;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Behaviors;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.WeeklyTrainingPlans;

// Self-contained factory (rather than the shared TestApiFactory, which
// hardcodes a non-null gym for every fake user) so it can authenticate as a
// Client with no gym and no coach - someone who signed up on their own.
public class IndependentClientTestApiFactory : WebApplicationFactory<Program>
{
    private static readonly string DbName = "FitRosIndependentClientTestDb";
    public static readonly Guid ClientUserId = Guid.NewGuid();
    public static readonly Guid ClientProfileId = Guid.NewGuid();
    public static readonly Guid OtherIndependentClientProfileId = Guid.NewGuid();
    public static readonly Guid OtherRoutineGymId = Guid.NewGuid();
    public static readonly Guid RoutineId = Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser>(_ => new FakeCurrentUser(null)
            {
                UserId = ClientUserId,
                GymId = null,
                Role = UserRole.Client,
                IsAuthenticated = true
            });

            services.AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.SchemeName, _ => { });

            services.RemoveAll<DbContextOptions<FitRosDbContext>>();
            services.RemoveAll<IFitRosDbContext>();
            services.AddDbContext<FitRosDbContext>(options =>
                options.UseInMemoryDatabase(DbName));
            services.AddScoped<IFitRosDbContext>(sp =>
                sp.GetRequiredService<FitRosDbContext>());

            services.RemoveAll(typeof(IPipelineBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthenticationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TenantGuardBehavior<,>));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<FitRosDbContext>();
        context.Database.EnsureCreated();

        if (!context.ClientProfiles.IgnoreQueryFilters().Any(p => p.Id == ClientProfileId))
        {
            var profile = ClientProfile.CreateIndependent(ClientUserId);
            typeof(ClientProfile).GetProperty(nameof(ClientProfile.Id))!
                .SetValue(profile, ClientProfileId);
            context.ClientProfiles.Add(profile);

            var otherProfile = ClientProfile.CreateIndependent(Guid.NewGuid());
            typeof(ClientProfile).GetProperty(nameof(ClientProfile.Id))!
                .SetValue(otherProfile, OtherIndependentClientProfileId);
            context.ClientProfiles.Add(otherProfile);

            // Gymless, same as the client - AssignRoutineToDay looks this up
            // through the ambient tenant filter, which only matches on an
            // exact GymId (including null == null for an independent user).
            var routine = WorkoutRoutine.Create(OtherRoutineGymId, "Full Body", "General routine");
            typeof(WorkoutRoutine).GetProperty(nameof(WorkoutRoutine.Id))!
                .SetValue(routine, RoutineId);
            typeof(WorkoutRoutine).GetProperty(nameof(WorkoutRoutine.GymId))!
                .SetValue(routine, null);
            context.WorkoutRoutines.Add(routine);

            context.SaveChanges();
        }

        return host;
    }
}

public class IndependentClientTrainingPlanApiTests : IClassFixture<IndependentClientTestApiFactory>
{
    private readonly HttpClient _client;

    public IndependentClientTrainingPlanApiTests(IndependentClientTestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Independent_Client_Can_Create_View_And_Manage_Their_Own_Plan()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/training-plans", new
        {
            clientProfileId = IndependentClientTestApiFactory.ClientProfileId,
            name = "My Own Plan"
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<CreateResponseDto>();
        created.Should().NotBeNull();

        var planId = created!.Id;

        var getResponse = await _client.GetAsync($"/api/training-plans/{planId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var renameResponse = await _client.PatchAsJsonAsync(
            $"/api/training-plans/{planId}/rename",
            new { name = "Renamed Plan" });
        renameResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var assignResponse = await _client.PutAsJsonAsync(
            $"/api/training-plans/{planId}/days/1",
            new { workoutRoutineId = IndependentClientTestApiFactory.RoutineId, notes = (string?)null });
        assignResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var activateResponse = await _client.PatchAsync($"/api/training-plans/{planId}/activate", null);
        activateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var activePlanResponse = await _client.GetAsync(
            $"/api/training-plans/client/{IndependentClientTestApiFactory.ClientProfileId}/active");
        activePlanResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var archiveResponse = await _client.PatchAsync($"/api/training-plans/{planId}/archive", null);
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Independent_Client_Cannot_Create_A_Plan_For_Another_Independent_Client()
    {
        var createForOther = await _client.PostAsJsonAsync("/api/training-plans", new
        {
            clientProfileId = IndependentClientTestApiFactory.OtherIndependentClientProfileId,
            name = "Not mine"
        });

        createForOther.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private sealed record CreateResponseDto(Guid Id);
}
