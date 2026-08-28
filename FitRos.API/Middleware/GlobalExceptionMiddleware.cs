using FitRos.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net;

namespace FitRos.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        // Exceptions whose Message is deliberately authored by our own domain /
        // validation code and is therefore safe to return to the client verbatim.
        var isClientSafe = ex is UnauthorizedException
            or ForbiddenException
            or NotFoundException
            or DomainException
            or ValidationException;

        var statusCode = ex switch
        {
            UnauthorizedException => HttpStatusCode.Unauthorized,      // 401
            ForbiddenException => HttpStatusCode.Forbidden,            // 403
            NotFoundException => HttpStatusCode.NotFound,              // 404
            KeyNotFoundException => HttpStatusCode.NotFound,           // 404
            DomainException => HttpStatusCode.BadRequest,              // 400
            ValidationException => HttpStatusCode.BadRequest,          // 400
            // Bad caller input that slipped past validation (e.g. a domain
            // ArgumentException.ThrowIfNullOrWhiteSpace). 400, but the message
            // can name internal parameters, so it is NOT treated as client-safe.
            ArgumentException => HttpStatusCode.BadRequest,            // 400
            DbUpdateConcurrencyException => HttpStatusCode.Conflict,   // 409
            _ => HttpStatusCode.InternalServerError                    // 500
        };

        // Log server faults (5xx) at Error with the full exception; anything the
        // client caused (4xx) is noise at Warning and carries no stack trace.
        if ((int)statusCode >= 500)
        {
            _logger.LogError(
                ex,
                "Unhandled exception for {Method} {Path} (traceId {TraceId})",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }
        else
        {
            _logger.LogWarning(
                "Request failed ({StatusCode}) for {Method} {Path}: {ExceptionType}",
                (int)statusCode,
                context.Request.Method,
                context.Request.Path,
                ex.GetType().Name);
        }

        // Only ever surface raw exception text when it is client-safe by design,
        // or when running in a developer environment. In production every other
        // failure returns a generic message keyed by the trace id.
        var exposeDetail = isClientSafe || _environment.IsDevelopment();

        var problemDetails = new ProblemDetails
        {
            Title = exposeDetail
                ? ex.GetType().Name
                : "ServerError",
            Detail = exposeDetail
                ? ex.Message
                : "An unexpected error occurred. Please contact support and quote the trace id.",
            Status = (int)statusCode,
            Instance = context.Request.Path
        };

        problemDetails.Extensions["traceId"] = context.TraceIdentifier;

        if (ex is ValidationException validationException)
        {
            problemDetails.Extensions["errors"] = validationException.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray()
                );
        }

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}
