using System.Net.Http;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Api.Cors;

/// <summary>
/// Boots the API with a single configured CORS origin (as Render supplies via
/// Cors__AllowedOrigins__0) to prove the policy is driven by configuration
/// rather than a hardcoded localhost string.
/// </summary>
public class CorsConfigTestApiFactory : WebApplicationFactory<Program>
{
    public const string AllowedOrigin = "https://fitros.pages.dev";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // UseSetting writes straight into the host configuration that Program.cs
        // reads at build time, unlike ConfigureAppConfiguration which lands too late.
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FitRosDbContext>>();
            services.RemoveAll<IFitRosDbContext>();
            services.AddDbContext<FitRosDbContext>(options =>
                options.UseInMemoryDatabase("FitRosCorsTestDb"));
            services.AddScoped<IFitRosDbContext>(sp =>
                sp.GetRequiredService<FitRosDbContext>());
        });
    }
}

public class CorsConfigTests : IClassFixture<CorsConfigTestApiFactory>
{
    private readonly CorsConfigTestApiFactory _factory;

    public CorsConfigTests(CorsConfigTestApiFactory factory) => _factory = factory;

    [Fact]
    public async Task Configured_origin_is_echoed_back()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", CorsConfigTestApiFactory.AllowedOrigin);

        var response = await client.SendAsync(request);

        response.Headers.GetValues("Access-Control-Allow-Origin")
            .Should().ContainSingle().Which.Should().Be(CorsConfigTestApiFactory.AllowedOrigin);
    }

    [Fact]
    public async Task Unlisted_origin_gets_no_cors_header()
    {
        var client = _factory.CreateClient();

        var request = new HttpRequestMessage(HttpMethod.Get, "/health");
        request.Headers.Add("Origin", "https://not-allowed.example.com");

        var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").Should().BeFalse();
    }
}
