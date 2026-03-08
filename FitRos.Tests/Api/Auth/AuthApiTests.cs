using FitRos.API;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using Xunit;

namespace FitRos.Tests.Api.Auth;

public class AuthApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthApiTests(WebApplicationFactory<Program> factory)
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