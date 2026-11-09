namespace FitRos.API.Configuration;

/// <summary>
/// Wraps <c>ConnectionStrings:DefaultConnection</c> so it can be validated at
/// startup like every other required setting.
/// </summary>
public sealed class DatabaseOptions
{
    public const string ConnectionStringName = "DefaultConnection";

    public string ConnectionString { get; set; } = string.Empty;
}
