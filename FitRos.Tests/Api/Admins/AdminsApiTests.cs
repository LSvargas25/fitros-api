using FitRos.API;
using FitRos.Application.Abstractions.Persistence;
using FitRos.Application.Abstractions.Security;
using FitRos.Application.Common.Behaviors;
using FitRos.Application.Features.Users.Admin.CreateAdmin;
using FitRos.Application.Features.Users.UserManagement.CreateUser;
using FitRos.Domain.Enums;
using FitRos.Infrastructure.Persistence;
using FitRos.Tests.Infrastructure;
using FitRos.Tests.TestDoubles;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Admins;

/// <summary>
/// Factory that authenticates as OwnerApp — required for admin lifecycle endpoints.
/// </summary>
public class OwnerAppTestApiFactory : WebApplicationFactory<Program>
{
    private static readonly string DbName = "FitRosOwnerAppTestDb";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<ICurrentUser>();
            services.AddScoped<ICurrentUser>(_ =>
                new FakeCurrentUser(null)
                {
                    UserId = Guid.Parse("33333333-3333-3333-3333-333333333333"),
                    GymId = null,
                    Role = UserRole.OwnerApp,
                    IsAuthenticated = true
                });

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
}

// ─────────────────────────────────────────────────────────────
// 403 tests — default TestApiFactory uses Role=Admin,
// which is not allowed on OwnerApp-only endpoints.
// ─────────────────────────────────────────────────────────────

public class AdminsApiTests_Forbidden : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public AdminsApiTests_Forbidden(TestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ActivateAdmin_Returns_403_When_Role_Is_Admin()
    {
        var response = await _client.PatchAsync(
            $"/api/admins/{Guid.NewGuid()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeactivateAdmin_Returns_403_When_Role_Is_Admin()
    {
        var response = await _client.PatchAsync(
            $"/api/admins/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAdmin_Returns_403_When_Role_Is_Admin()
    {
        var response = await _client.DeleteAsync(
            $"/api/admins/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DeleteAdminPermanent_Returns_403_When_Role_Is_Admin()
    {
        var response = await _client.DeleteAsync(
            $"/api/admins/{Guid.NewGuid()}/permanent");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

// ─────────────────────────────────────────────────────────────
// Success + 404 tests — OwnerApp factory
// ─────────────────────────────────────────────────────────────

public class AdminsApiTests_OwnerApp : IClassFixture<OwnerAppTestApiFactory>
{
    private readonly HttpClient _client;

    public AdminsApiTests_OwnerApp(OwnerAppTestApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ─── helpers ───────────────────────────────────────────────

    private async Task<CreateUserResponse> CreateAdminAsync(string email)
    {
        var command = new CreateAdminCommand(email, "Test", "Admin", "Password123!");
        var response = await _client.PostAsJsonAsync("/api/admins", command);
        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CreateUserResponse>())!;
    }

    private async Task DeactivateAdminAsync(Guid id)
    {
        var response = await _client.PatchAsync($"/api/admins/{id}/deactivate", null);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }

    // ─── 404 tests ─────────────────────────────────────────────

    [Fact]
    public async Task ActivateAdmin_Returns_404_When_Admin_Not_Found()
    {
        var response = await _client.PatchAsync(
            $"/api/admins/{Guid.NewGuid()}/activate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeactivateAdmin_Returns_404_When_Admin_Not_Found()
    {
        var response = await _client.PatchAsync(
            $"/api/admins/{Guid.NewGuid()}/deactivate", null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteAdmin_Returns_404_When_Admin_Not_Found()
    {
        var response = await _client.DeleteAsync(
            $"/api/admins/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ─── 204 success tests ─────────────────────────────────────

    [Fact]
    public async Task DeactivateAdmin_Returns_204_On_Success()
    {
        var admin = await CreateAdminAsync($"{Guid.NewGuid()}@deactivate.com");

        var response = await _client.PatchAsync(
            $"/api/admins/{admin.Id}/deactivate", null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task ActivateAdmin_Returns_204_On_Success()
    {
        var admin = await CreateAdminAsync($"{Guid.NewGuid()}@activate.com");
        await DeactivateAdminAsync(admin.Id);

        var response = await _client.PatchAsync(
            $"/api/admins/{admin.Id}/activate", null);
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task DeleteAdmin_Returns_204_On_Success()
    {
        // Create two admins so we don't delete the last one
        await CreateAdminAsync($"{Guid.NewGuid()}@keeper.com");
        var adminToDelete = await CreateAdminAsync($"{Guid.NewGuid()}@delete.com");

        await DeactivateAdminAsync(adminToDelete.Id);

        var response = await _client.DeleteAsync(
            $"/api/admins/{adminToDelete.Id}");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }

    [Fact]
    public async Task DeleteAdminPermanent_Returns_204_On_Success()
    {
        await CreateAdminAsync($"{Guid.NewGuid()}@keeper2.com");
        var adminToDelete = await CreateAdminAsync($"{Guid.NewGuid()}@deleteperm.com");

        await DeactivateAdminAsync(adminToDelete.Id);

        var response = await _client.DeleteAsync(
            $"/api/admins/{adminToDelete.Id}/permanent");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, body);
    }
}
