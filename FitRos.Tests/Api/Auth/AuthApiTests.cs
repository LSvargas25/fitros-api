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
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Auth;

// Uses a dedicated in-memory-database factory (rather than the raw
// WebApplicationFactory<Program>, which points at the real Postgres
// connection string) so these tests don't require a live database. Unlike
// FitRos.Tests.Infraestructure.TestApiFactory, ICurrentUser is left wired to
// its real JWT-backed implementation, since these tests exercise actual
// authentication behavior (e.g. Logout_Should_Return_401_When_User_Not_Authenticated).
public class AuthTestApiFactory : WebApplicationFactory<Program>
{
    private static readonly string DbName = "FitRosAuthTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FitRosDbContext>>();
            services.RemoveAll<IFitRosDbContext>();
            services.AddDbContext<FitRosDbContext>(options =>
                options.UseInMemoryDatabase(DbName));
            services.AddScoped<IFitRosDbContext>(sp =>
                sp.GetRequiredService<FitRosDbContext>());

            // Avoid depending on a live SMTP server for ForgotPassword's email.
            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender, FakeEmailSender>();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        // The InMemory provider doesn't apply model HasData seeds (e.g. the
        // seeded owner account) on lazy first access the way relational
        // providers do via migrations - EnsureCreated triggers that seeding.
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<FitRosDbContext>().Database.EnsureCreated();

        return host;
    }
}

public class AuthApiTests : IClassFixture<AuthTestApiFactory>
{
    private readonly HttpClient _client;

    public AuthApiTests(AuthTestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // =============================
    // LOGIN
    // =============================

    [Fact]
    public async Task Login_Should_Return_200_When_Credentials_Are_Valid()
    {
        var request = new
        {
            email = "owner@fitros.com",
            password = "Owner123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_Should_Return_400_When_Email_Is_Missing()
    {
        var request = new
        {
            email = "",
            password = "Owner123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/login",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =============================
    // FORGOT PASSWORD
    // =============================

    [Fact]
    public async Task ForgotPassword_Should_Return_200()
    {
        var request = new
        {
            email = "owner@fitros.com"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ForgotPassword_Should_Return_200_Even_When_Email_Does_Not_Exist()
    {
        var request = new
        {
            email = "nonexisting@email.com"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/forgot-password",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // =============================
    // RESET PASSWORD
    // =============================

    [Fact]
    public async Task ResetPassword_Should_Return_400_When_Token_Is_Invalid()
    {
        var request = new
        {
            email = "owner@fitros.com",
            token = "invalid-token",
            newPassword = "NewPassword123!"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/reset-password",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =============================
    // REFRESH TOKEN
    // =============================

    [Fact]
    public async Task Refresh_Should_Return_400_When_RefreshToken_Is_Invalid()
    {
        var request = new
        {
            refreshToken = "invalid-token"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/refresh",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // =============================
    // LOGOUT
    // =============================

    [Fact]
    public async Task Logout_Should_Return_401_When_User_Not_Authenticated()
    {
        var request = new
        {
            refreshToken = "some-token"
        };

        var response = await _client.PostAsJsonAsync(
            "/api/auth/logout",
            request);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}