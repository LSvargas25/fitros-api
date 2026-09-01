using FitRos.Application.Features.Exercises.CreateExercise;
using FitRos.Application.Features.Users.UserManagement.UserList;
using FitRos.Domain.Entities.Enums;
using FitRos.Domain.Entities.Users;
using FitRos.Domain.Enums;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Exercises;

public class ExercisesApiTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public ExercisesApiTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Create_Should_Return_201()
    {
        var request = new CreateExerciseCommand(
            $"Bench Press {Guid.NewGuid()}",
            "Chest exercise",
            MuscleGroup.Chest
        );

        var response = await _client.PostAsJsonAsync(
            "/api/exercises",
            request);

        var body = await response.Content.ReadAsStringAsync();

        Console.WriteLine(body);

        response.StatusCode.Should().Be(HttpStatusCode.Created, body);
    }

    [Fact]
    public async Task Get_Should_Return_200()
    {
        var response = await _client.GetAsync("/api/exercises");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
    [Fact]
    public async Task Should_Return_User_When_Exists()
    {
        var context = TestDbContextFactory.Create();

        var gymId = Guid.NewGuid();

        var user = User.CreateForGym(
            gymId,
            "test@test.com",
            "Luis",
            "Vargas",
            "hash",
            UserRole.Client);

        context.Users.Add(user);
        await context.SaveChangesAsync(CancellationToken.None);

        var fakeCurrentUser = new FakeCurrentUser(gymId)
        {
            UserId = Guid.NewGuid(),
            Role = UserRole.Admin,
            IsAuthenticated = true
        };

        var handler = new GetUserByIdHandler(context, fakeCurrentUser);

        var result = await handler.Handle(
            new GetUserByIdQuery(user.Id),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Email.Should().Be("test@test.com");
    }

}