namespace FitRos.API.Middleware;

/// <summary>
/// Adds defensive HTTP response headers to every response. HSTS is handled
/// separately by <c>app.UseHsts()</c> (it must only be emitted over HTTPS).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    // A JSON API never needs to load scripts, styles, frames or images, so the
    // default policy locks everything down. Swagger UI (Development only) does,
    // so it gets a slightly looser policy below.
    private const string ApiContentSecurityPolicy =
        "default-src 'none'; frame-ancestors 'none'; base-uri 'none'; form-action 'none'";

    private const string SwaggerContentSecurityPolicy =
        "default-src 'self'; img-src 'self' data:; style-src 'self' 'unsafe-inline'; " +
        "script-src 'self' 'unsafe-inline'; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'";

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var headers = ctx.Response.Headers;

            headers["X-Content-Type-Options"] = "nosniff";
            headers["X-Frame-Options"] = "DENY";
            headers["Referrer-Policy"] = "no-referrer";
            headers["X-Permitted-Cross-Domain-Policies"] = "none";
            headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=(), browsing-topics=()";
            headers["Cross-Origin-Resource-Policy"] = "same-origin";

            var isSwagger = ctx.Request.Path.StartsWithSegments("/swagger");
            headers["Content-Security-Policy"] =
                isSwagger ? SwaggerContentSecurityPolicy : ApiContentSecurityPolicy;

            // Kestrel's advertising banner buys an attacker free recon.
            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        }, context);

        return _next(context);
    }
}
