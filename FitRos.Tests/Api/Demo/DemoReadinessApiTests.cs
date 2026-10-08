using System.Net;
using System.Net.Http.Json;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Infrastructure.Persistence;
using FitRos.Infrastructure.Persistence.Seeding;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FitRos.Tests.Api.Demo;

/// <summary>
/// Host configured like the Render deploy (Production environment, real JWT
/// auth) with its own in-memory database; the switches under test are passed
/// as settings, the same keys Render sets as env vars.
/// </summary>
public abstract class ProductionLikeApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"FitRosProdLike-{Guid.NewGuid()}";

    protected abstract IReadOnlyDictionary<string, string> Settings { get; }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");

        foreach (var (key, value) in Settings)
            builder.UseSetting(key, value);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FitRosDbContext>>();
            services.RemoveAll<IFitRosDbContext>();
            services.AddDbContext<FitRosDbContext>(options => options.UseInMemoryDatabase(_dbName));
            services.AddScoped<IFitRosDbContext>(sp => sp.GetRequiredService<FitRosDbContext>());
        });
    }

    public HttpClient CreateHttpsClient() => CreateClient(new WebApplicationFactoryClientOptions
    {
        BaseAddress = new Uri("https://localhost")
    });
}

public sealed class DemoEnabledApiFactory : ProductionLikeApiFactory
{
    protected override IReadOnlyDictionary<string, string> Settings { get; } = new Dictionary<string, string>
    {
        ["Seed:Demo"] = "true",
        ["Swagger:Enabled"] = "true",
    };
}

public sealed class DemoDisabledApiFactory : ProductionLikeApiFactory
{
    protected override IReadOnlyDictionary<string, string> Settings { get; } = new Dictionary<string, string>();
}

public class DemoEnabledApiTests : IClassFixture<DemoEnabledApiFactory>
{
    private readonly HttpClient _client;

    public DemoEnabledApiTests(DemoEnabledApiFactory factory) => _client = factory.CreateHttpsClient();

    [Theory]
    [InlineData(DemoDataSeeder.OwnerEmail)]
    [InlineData(DemoDataSeeder.AdminEmail)]
    [InlineData(DemoDataSeeder.CoachEmail)]
    [InlineData(DemoDataSeeder.ClientEmail)]
    public async Task Seed_Demo_true_makes_every_demo_login_work(string email)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email, password = DemoDataSeeder.DemoPassword });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("accessToken");
    }

    [Fact]
    public async Task Health_ready_returns_200_without_auth_when_the_database_answers()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
    }

    [Fact]
    public async Task Swagger_Enabled_true_serves_swagger_in_production()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}

public class DemoDisabledApiTests : IClassFixture<DemoDisabledApiFactory>
{
    private readonly HttpClient _client;

    public DemoDisabledApiTests(DemoDisabledApiFactory factory) => _client = factory.CreateHttpsClient();

    [Fact]
    public async Task Without_Seed_Demo_no_demo_account_exists()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login",
            new { email = DemoDataSeeder.AdminEmail, password = DemoDataSeeder.DemoPassword });

        response.IsSuccessStatusCode.Should().BeFalse();
    }

    [Fact]
    public async Task Swagger_is_off_in_production_by_default()
    {
        var response = await _client.GetAsync("/swagger/v1/swagger.json");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Health_ready_is_reachable_without_auth()
    {
        var response = await _client.GetAsync("/health/ready");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
