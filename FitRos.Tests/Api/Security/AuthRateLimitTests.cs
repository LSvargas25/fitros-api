using System.Net;
using System.Net.Http.Json;
using FitRos.API;
using FitRos.Application.Abstractions.Messaging;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace FitRos.Tests.Api.Security;

public class RateLimitedApiFactory : WebApplicationFactory<Program>
{
    public const int PermitLimit = 3;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("RateLimiting:Auth:PermitLimit", PermitLimit.ToString());
        builder.UseSetting("RateLimiting:Auth:WindowSeconds", "60");

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FitRosDbContext>>();
            services.RemoveAll<IFitRosDbContext>();
            services.AddDbContext<FitRosDbContext>(options =>
                options.UseInMemoryDatabase("FitRosRateLimitTestDb"));
            services.AddScoped<IFitRosDbContext>(sp =>
                sp.GetRequiredService<FitRosDbContext>());

            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender, FakeEmailSender>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<FitRosDbContext>().Database.EnsureCreated();
        return host;
    }
}

// A fresh factory (hence a fresh rate-limiter partition) per test method, so
// the login test and the forgot-password test don't consume each other's window.
public class AuthRateLimitTests : IDisposable
{
    private readonly RateLimitedApiFactory _factory = new();
    private readonly HttpClient _client;

    public AuthRateLimitTests()
    {
        _client = _factory.CreateClient();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Login_is_throttled_with_429_once_the_permit_limit_is_exceeded()
    {
        var body = new { email = "nobody@fitros.com", password = "wrong-password" };

        // The first PermitLimit attempts are allowed through (and fail auth on
        // their own merits); the next one is rejected by the rate limiter.
        for (var i = 0; i < RateLimitedApiFactory.PermitLimit; i++)
        {
            var allowed = await _client.PostAsJsonAsync("/api/auth/login", body);
            allowed.StatusCode.Should().NotBe(HttpStatusCode.TooManyRequests);
        }

        var throttled = await _client.PostAsJsonAsync("/api/auth/login", body);

        throttled.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Forgot_password_shares_the_same_throttle()
    {
        HttpResponseMessage? last = null;

        for (var i = 0; i < RateLimitedApiFactory.PermitLimit + 2; i++)
            last = await _client.PostAsJsonAsync(
                "/api/auth/forgot-password",
                new { email = "nobody@fitros.com" });

        last!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
