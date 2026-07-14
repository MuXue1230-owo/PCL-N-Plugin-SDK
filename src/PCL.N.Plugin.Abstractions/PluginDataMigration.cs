namespace PCL.N.Plugin;

public enum PluginDataClassification
{
    LocalOnly,
    Syncable,
    Secret,
    Cache,
    Generated,
    Temporary
}

/// <summary>A single forward-only data schema migration declared in plugin.json.</summary>
public interface IPluginDataMigration
{
    string Id { get; }

    int FromSchemaVersion { get; }

    int ToSchemaVersion { get; }

    ValueTask ApplyAsync(
        IPluginMigrationContext context,
        CancellationToken cancellationToken);
}

/// <summary>
/// Migration paths are transaction-owned. Plugins may only modify <see cref="WorkingDirectory"/>;
/// the host commits it atomically after every declared migration succeeds.
/// </summary>
public interface IPluginMigrationContext
{
    string PluginId { get; }

    string WorkingDirectory { get; }

    string BackupDirectory { get; }

    int FromSchemaVersion { get; }

    int ToSchemaVersion { get; }
}

public enum PluginHealthStatus
{
    Healthy,
    Degraded,
    Unhealthy
}

public sealed record PluginHealthResult(
    PluginHealthStatus Status,
    string? Message = null)
{
    public static PluginHealthResult Healthy(string? message = null) =>
        new(PluginHealthStatus.Healthy, message);

    public static PluginHealthResult Degraded(string? message = null) =>
        new(PluginHealthStatus.Degraded, message);

    public static PluginHealthResult Unhealthy(string? message = null) =>
        new(PluginHealthStatus.Unhealthy, message);
}

public interface IPluginHealthCheck
{
    ValueTask<PluginHealthResult> CheckAsync(CancellationToken cancellationToken);
}
