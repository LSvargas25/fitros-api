using System.Net;
using FitRos.Tests.Infrastructure;
using FluentAssertions;
using Xunit;

namespace FitRos.Tests.Api.Security;

// Every non-Auth controller must reject an unauthenticated request. The shared
// TestApiFactory authenticates by default; the X-Test-Unauthenticated header
// opts a single request out so we can assert the 401.
public class ControllerAuthorizationTests : IClassFixture<TestApiFactory>
{
    private readonly TestApiFactory _factory;

    public ControllerAuthorizationTests(TestApiFactory factory)
    {
        _factory = factory;
    }

    // The controllers whose [Authorize] was added/normalised in the item-1 pass,
    // plus WorkoutRoutines (whose [FromServices] actions rely entirely on the
    // controller-level [Authorize]).
    public static IEnumerable<object[]> ProtectedEndpoints => new[]
    {
        new object[] { HttpMethod.Get, "/api/admins" },
        new object[] { HttpMethod.Get, "/api/clients" },
        new object[] { HttpMethod.Get, "/api/coaches" },
        new object[] { HttpMethod.Get, "/api/exercises" },
        new object[] { HttpMethod.Get, "/api/users" },
        new object[] { HttpMethod.Get, "/api/dashboard" },
        new object[] { HttpMethod.Get, "/api/workoutroutines" },
    };

    [Theory]
    [MemberData(nameof(ProtectedEndpoints))]
    public async Task Unauthenticated_request_is_rejected(HttpMethod method, string path)
    {
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(method, path);
        request.Headers.Add(TestAuthHandler.UnauthenticatedHeader, "1");

        var response = await client.SendAsync(request);

        response.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden);
    }
}
