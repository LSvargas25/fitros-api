using System.Net;
using System.Net.Http.Json;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Api.Security;

// A Client must not reach staff-only controllers. TestAuthHandler honours an
// X-Test-Role header so we can present a Client principal to the MVC gate.
public class ClientRoleRejectionTests : IClassFixture<TestApiFactory>
{
    private readonly HttpClient _client;

    public ClientRoleRejectionTests(TestApiFactory factory)
    {
        _client = factory.CreateClient();
        _client.DefaultRequestHeaders.Add(TestAuthHandler.RoleHeader, "Client");
    }

    public static IEnumerable<object[]> StaffOnlyEndpoints => new[]
    {
        new object[] { "GET",  "/api/exercises" },
        new object[] { "POST", "/api/exercises" },
        new object[] { "GET",  "/api/foods" },
        new object[] { "POST", "/api/foods" },
        new object[] { "POST", "/api/meal-plans" },
        new object[] { "GET",  "/api/meal-plans/" + Guid.Empty },
        new object[] { "POST", "/api/training-plans" },
        new object[] { "GET",  "/api/training-plans/" + Guid.Empty },
        new object[] { "GET",  "/api/workoutroutines" },
        new object[] { "GET",  "/api/admins" },
        new object[] { "GET",  "/api/coaches" },
        new object[] { "GET",  "/api/clients" },
        new object[] { "GET",  "/api/gyms" },
        new object[] { "GET",  "/api/dashboard" },
    };

    [Theory]
    [MemberData(nameof(StaffOnlyEndpoints))]
    public async Task Client_is_forbidden(string method, string path)
    {
        var request = new HttpRequestMessage(new HttpMethod(method), path);
        if (method == "POST")
            request.Content = JsonContent.Create(new { });

        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            $"{method} {path} is staff-only");
    }
}
