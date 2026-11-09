using FitRos.Infrastructure.Messaging;
using FitRos.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace FitRos.API.Configuration;

public static class RequiredSettingsExtensions
{
    /// <summary>
    /// Prefix of the placeholders committed in appsettings.json. A value that
    /// still contains it was never overridden by an env var or user-secret.
    /// </summary>
    public const string PlaceholderPrefix = "REPLACE_VIA_ENV_VAR_";

    /// <summary>
    /// Binds the settings that carry secrets and validates them when the host
    /// starts, so a missing env var fails the boot with a message naming the
    /// variable to set instead of failing later on the first request.
    /// </summary>
    public static IServiceCollection AddRequiredSettings(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddOptions<DatabaseOptions>()
            .Configure(o => o.ConnectionString =
                configuration.GetConnectionString(DatabaseOptions.ConnectionStringName) ?? string.Empty)
            .Validate(o => IsConfigured(o.ConnectionString),
                Missing("ConnectionStrings:DefaultConnection", "ConnectionStrings__DefaultConnection"))
            .ValidateOnStart();

        services
            .AddOptions<JwtSettings>()
            .Bind(configuration.GetSection("Jwt"))
            .Validate(o => IsConfigured(o.Issuer), Missing("Jwt:Issuer", "Jwt__Issuer"))
            .Validate(o => IsConfigured(o.Audience), Missing("Jwt:Audience", "Jwt__Audience"))
            .Validate(o => IsConfigured(o.SigningKey), Missing("Jwt:SigningKey", "Jwt__SigningKey"))
            .ValidateOnStart();

        services
            .AddOptions<SmtpSettings>()
            .Bind(configuration.GetSection("Smtp"))
            .Validate(o => IsConfigured(o.Host), Missing("Smtp:Host", "Smtp__Host"))
            .Validate(o => IsConfigured(o.Username), Missing("Smtp:Username", "Smtp__Username"))
            .Validate(o => IsConfigured(o.Password), Missing("Smtp:Password", "Smtp__Password"))
            .Validate(o => IsConfigured(o.From), Missing("Smtp:From", "Smtp__From"))
            .ValidateOnStart();

        services
            .AddOptions<GoogleAuthSettings>()
            .Bind(configuration.GetSection("Authentication:Google"))
            .Validate(o => IsConfigured(o.ClientId),
                Missing("Authentication:Google:ClientId", "Authentication__Google__ClientId"))
            .ValidateOnStart();

        return services;
    }

    public static bool IsConfigured(string? value) =>
        !string.IsNullOrWhiteSpace(value)
        && !value.Contains(PlaceholderPrefix, StringComparison.Ordinal);

    private static string Missing(string key, string envVar) =>
        $"{key} is not configured (empty or still a {PlaceholderPrefix}* placeholder). "
        + $"Set the {envVar} environment variable, or the '{key}' user-secret.";
}
