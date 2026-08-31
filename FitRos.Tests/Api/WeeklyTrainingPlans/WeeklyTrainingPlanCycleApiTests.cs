using System.Net;
using System.Net.Http.Json;
using FitRos.API.Contracts.WeeklyTrainingPlans;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Features.WeeklyTrainingPlans.CreatePlan;
using FitRos.Application.Features.WeeklyTrainingPlans.GetPlanById;
using FitRos.Domain.Entities.Client;
using FitRos.Domain.Entities.Training;
using FitRos.Domain.Entities.WeeklyTraining;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace FitRos.Tests.Api.WeeklyTrainingPlans;

// End-to-end over HTTP (authenticated Admin, gym 11111111-…): the full weekly-plan
// cycle, the Published-routine rule, and the one-active-plan rule.
public class WeeklyTrainingPlanCycleApiTests : IClassFixture<TestApiFactory>
{
    private static readonly Guid TestGymId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly TestApiFactory _factory;
    private readonly HttpClient _client;

    public WeeklyTrainingPlanCycleApiTests(TestApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    private async Task<(Guid profileId, Guid publishedRoutineId, Guid draftRoutineId)> SeedAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var db = (FitRosDbContext)scope.ServiceProvider.GetRequiredService<IFitRosDbContext>();

        var profile = ClientProfile.Create(TestGymId, Guid.NewGuid(), Guid.NewGuid());
        db.ClientProfiles.Add(profile);

        var published = WorkoutRoutine.Create(TestGymId, $"Published {Guid.NewGuid()}", "x");
        published.AddExercise(Guid.NewGuid(), 1, 3, 10, 60);
        published.Publish();
        var draft = WorkoutRoutine.Create(TestGymId, $"Draft {Guid.NewGuid()}", "x");
        db.WorkoutRoutines.AddRange(published, draft);

        await db.SaveChangesAsync(CancellationToken.None);
        return (profile.Id, published.Id, draft.Id);
    }

    [Fact]
    public async Task Full_cycle_create_assign_published_activate()
    {
        var (profileId, publishedRoutineId, _) = await SeedAsync();

        var create = await _client.PostAsJsonAsync("/api/training-plans",
            new CreateWeeklyTrainingPlanCommand(profileId, "Mesocycle 1"));
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
        var planId = (await create.Content.ReadFromJsonAsync<CreateWeeklyTrainingPlanResponse>())!.Id;

        // Monday = 1 in System.DayOfWeek
        var assign = await _client.PutAsJsonAsync($"/api/training-plans/{planId}/days/1",
            new AssignRoutineToDayRequest(publishedRoutineId, "Lower"));
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent, await assign.Content.ReadAsStringAsync());

        var activate = await _client.PatchAsync($"/api/training-plans/{planId}/activate", null);
        activate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var active = await _client.GetFromJsonAsync<WeeklyTrainingPlanDto>(
            $"/api/training-plans/client/{profileId}/active");
        active!.Id.Should().Be(planId);
        active.Days.Should().ContainSingle()
            .Which.WorkoutRoutineId.Should().Be(publishedRoutineId);
    }

    [Fact]
    public async Task Assigning_a_draft_routine_is_400()
    {
        var (profileId, _, draftRoutineId) = await SeedAsync();

        var create = await _client.PostAsJsonAsync("/api/training-plans",
            new CreateWeeklyTrainingPlanCommand(profileId, "Meso"));
        var planId = (await create.Content.ReadFromJsonAsync<CreateWeeklyTrainingPlanResponse>())!.Id;

        var assign = await _client.PutAsJsonAsync($"/api/training-plans/{planId}/days/1",
            new AssignRoutineToDayRequest(draftRoutineId, null));

        assign.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Activating_a_second_plan_archives_the_first()
    {
        var (profileId, _, _) = await SeedAsync();

        async Task<Guid> NewPlan(string name)
        {
            var r = await _client.PostAsJsonAsync("/api/training-plans",
                new CreateWeeklyTrainingPlanCommand(profileId, name));
            return (await r.Content.ReadFromJsonAsync<CreateWeeklyTrainingPlanResponse>())!.Id;
        }

        var firstId = await NewPlan("First");
        var secondId = await NewPlan("Second");

        (await _client.PatchAsync($"/api/training-plans/{firstId}/activate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await _client.PatchAsync($"/api/training-plans/{secondId}/activate", null))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        var first = await _client.GetFromJsonAsync<WeeklyTrainingPlanDto>($"/api/training-plans/{firstId}");
        var second = await _client.GetFromJsonAsync<WeeklyTrainingPlanDto>($"/api/training-plans/{secondId}");

        first!.Status.Should().Be(FitRos.Domain.Entities.Enums.TrainingPlanStatus.Archived);
        second!.Status.Should().Be(FitRos.Domain.Entities.Enums.TrainingPlanStatus.Active);
    }
}
