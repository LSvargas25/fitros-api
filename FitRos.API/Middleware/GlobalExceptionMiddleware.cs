using System.Net;
using FluentValidation;
using FitRos.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace FitRos.API.Middleware;

public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public GlobalExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
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

    private static async Task HandleExceptionAsync(HttpContext context, Exception ex)
    {
        var statusCode = ex switch
        {
            UnauthorizedException => HttpStatusCode.Unauthorized, //  401
            ForbiddenException => HttpStatusCode.Forbidden,       //  403

            DomainException => HttpStatusCode.BadRequest,         //  400
            ValidationException => HttpStatusCode.BadRequest,     //  400
            InvalidOperationException => HttpStatusCode.BadRequest,
            KeyNotFoundException => HttpStatusCode.NotFound,      //  404

            _ => HttpStatusCode.InternalServerError               //  500
        };

        var problemDetails = new ProblemDetails
        {
            Title = ex.GetType().Name,
            Detail = ex.Message, 
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