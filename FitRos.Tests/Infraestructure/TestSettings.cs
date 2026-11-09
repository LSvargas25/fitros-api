using System.Runtime.CompilerServices;

namespace FitRos.Tests.Infrastructure;

/// <summary>
/// The API refuses to start while a required setting is empty or still a
/// REPLACE_VIA_ENV_VAR_* placeholder. CI has no secrets, so before any test
/// host boots this supplies dummy values the same way Render does: as env
/// vars. Values already set in the environment are left alone.
/// </summary>
internal static class TestSettings
{
    public static readonly IReadOnlyDictionary<string, string> Values = new Dictionary<string, string>
    {
        ["ConnectionStrings__DefaultConnection"] =
            "Host=localhost;Port=5432;Database=fitros_test;Username=postgres;Password=test",
        ["Jwt__SigningKey"] = "test-signing-key-that-is-at-least-32-bytes-long",
        ["Smtp__Username"] = "tests@fitros.test",
        ["Smtp__Password"] = "test-smtp-password",
        ["Smtp__From"] = "tests@fitros.test",
        ["Authentication__Google__ClientId"] = "test-google-client-id",
    };

    [ModuleInitializer]
    internal static void Initialize()
    {
        foreach (var (name, value) in Values)
        {
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(name)))
                Environment.SetEnvironmentVariable(name, value);
        }
    }
}
