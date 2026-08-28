using System.Text.Json;
using FitRos.API.Middleware;
using FitRos.Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace FitRos.Tests.Api.Middleware;

public class GlobalExceptionMiddlewareTests
{
    private static async Task<(int status, JsonElement body)> InvokeWith(
        Exception thrown,
        string environmentName)
    {
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/things";
        context.Request.Method = "POST";
        var responseBody = new MemoryStream();
        context.Response.Body = responseBody;

        var env = new StubHostEnvironment { EnvironmentName = environmentName };

        var middleware = new GlobalExceptionMiddleware(
            _ => throw thrown,
            NullLogger<GlobalExceptionMiddleware>.Instance,
            env);

        await middleware.InvokeAsync(context);

        responseBody.Position = 0;
        using var reader = new StreamReader(responseBody);
        var raw = await reader.ReadToEndAsync();
        using var json = JsonDocument.Parse(raw);
        return (context.Response.StatusCode, json.RootElement.Clone());
    }

    [Fact]
    public async Task DomainException_returns_400_with_its_message_even_in_production()
    {
        var (status, body) = await InvokeWith(
            new DomainException("Food is already archived."),
            Environments.Production);

        Assert.Equal(400, status);
        Assert.Equal("DomainException", body.GetProperty("title").GetString());
        Assert.Equal("Food is already archived.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task NotFoundException_returns_404_with_its_message_in_production()
    {
        var (status, body) = await InvokeWith(
            new NotFoundException("Resource not found."),
            Environments.Production);

        Assert.Equal(404, status);
        Assert.Equal("Resource not found.", body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unexpected_exception_in_production_is_500_and_does_not_leak_the_message()
    {
        var secret = "Nullable object must have a value.";

        var (status, body) = await InvokeWith(
            new InvalidOperationException(secret),
            Environments.Production);

        Assert.Equal(500, status);
        Assert.Equal("ServerError", body.GetProperty("title").GetString());
        Assert.DoesNotContain(secret, body.GetProperty("detail").GetString());
        Assert.Equal(
            "An unexpected error occurred. Please contact support and quote the trace id.",
            body.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task Unexpected_exception_in_development_still_shows_the_message()
    {
        var (status, body) = await InvokeWith(
            new InvalidOperationException("boom: internal detail"),
            Environments.Development);

        Assert.Equal(500, status);
        Assert.Equal("boom: internal detail", body.GetProperty("detail").GetString());
    }

    private sealed class StubHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "FitRos.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
