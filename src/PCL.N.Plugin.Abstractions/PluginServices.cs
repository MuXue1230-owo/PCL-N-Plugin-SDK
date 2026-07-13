namespace PCL.N.Plugin;

/// <summary>Stable service identifier (e.g. <c>pcl.settings</c>).</summary>
public readonly record struct PluginServiceId
{
    public PluginServiceId(string value)
    {
        if (!PluginId.TryParse(value, out _))
            throw new ArgumentException("Service ID must match ^[a-z0-9]+([.-][a-z0-9]+)*$.", nameof(value));
        Value = value;
    }

    public string Value { get; }

    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Well-known host service IDs (design §9 Stable surface; versions still 0.x until API 1.0).</summary>
public static class PluginServiceIds
{
    public static PluginServiceId Logging { get; } = new("pcl.logging");

    public static PluginServiceId Dispatcher { get; } = new("pcl.dispatcher");

    public static PluginServiceId Notifications { get; } = new("pcl.notifications");

    public static PluginServiceId Settings { get; } = new("pcl.settings");

    public static PluginServiceId Commands { get; } = new("pcl.commands");

    public static PluginServiceId Tasks { get; } = new("pcl.tasks");

    public static PluginServiceId InstancesRead { get; } = new("pcl.instances.read");

    public static PluginServiceId Ui { get; } = new("pcl.ui");

    public static PluginServiceId UiPatch { get; } = new("pcl.ui.patch");

    /// <summary>Host-managed access to the PCL.N plugin market.</summary>
    public static PluginServiceId Market { get; } = new("pcl.market");
}

/// <summary>Host-provided stable service exposed to third-party plugins.</summary>
public interface IPluginService
{
    PluginServiceId Id { get; }

    PluginApiVersion Version { get; }
}

/// <summary>Resolves stable services declared in the plugin Manifest.</summary>
public interface IPluginServiceProvider
{
    bool TryGet<TService>(out TService? service)
        where TService : class, IPluginService;

    TService Require<TService>()
        where TService : class, IPluginService;

    bool Supports(PluginServiceId serviceId, PluginApiVersionRange versionRange);
}

/// <summary>Structured plugin logger (also available as <see cref="IPluginContext.Logger"/>).</summary>
public interface IPluginLogger
{
    void Trace(string message);

    void Debug(string message);

    void Info(string message);

    void Warn(string message);

    void Error(string message, Exception? exception = null);
}

/// <summary>Dispatches work onto the host UI / main thread when required.</summary>
public interface IPluginDispatcher
{
    void Post(Action action);

    Task InvokeAsync(Action action, CancellationToken cancellationToken = default);

    Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken = default);
}

/// <summary>Host toast / non-modal notification surface (design §9).</summary>
public interface IPluginNotificationService : IPluginService
{
    void ShowInformation(string message);

    void ShowWarning(string message);
}

/// <summary>Typed key for the per-plugin settings store.</summary>
public readonly record struct PluginSettingKey<T>
{
    public PluginSettingKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Setting key cannot be empty.", nameof(name));
        if (name.Contains('/') || name.Contains('\\') || name.Contains("..", StringComparison.Ordinal))
            throw new ArgumentException("Setting key must be a simple name.", nameof(name));
        Name = name.Trim();
    }

    public string Name { get; }

    public override string ToString() => Name;
}

/// <summary>Isolated per-plugin key/value store under the plugin data directory.</summary>
public interface IPluginSettingsStore : IPluginService
{
    ValueTask<T> GetAsync<T>(
        PluginSettingKey<T> key,
        T defaultValue,
        CancellationToken cancellationToken = default);

    ValueTask SetAsync<T>(
        PluginSettingKey<T> key,
        T value,
        CancellationToken cancellationToken = default);
}

/// <summary>Host command palette / action registration (design §9.3).</summary>
public sealed record PluginCommandDescriptor
{
    public PluginCommandDescriptor(
        string id,
        string title,
        Func<CancellationToken, Task> executeAsync,
        string? description = null,
        string? icon = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(executeAsync);
        Id = id.Trim();
        Title = title.Trim();
        ExecuteAsync = executeAsync;
        Description = description;
        Icon = icon;
    }

    public string Id { get; init; }

    public string Title { get; init; }

    public Func<CancellationToken, Task> ExecuteAsync { get; init; }

    public string? Description { get; init; }

    public string? Icon { get; init; }
}

public interface IPluginCommandService : IPluginService
{
    IPluginRegistration Register(PluginCommandDescriptor descriptor);

    /// <summary>Invokes a previously registered command by id (host/test helper).</summary>
    Task InvokeAsync(string commandId, CancellationToken cancellationToken = default);
}

/// <summary>Tracked background work owned by the plugin lifetime (design §9.5).</summary>
public interface IPluginTaskRegistration : IPluginRegistration
{
    Task Completion { get; }
}

public interface IPluginTaskService : IPluginService
{
    IPluginTaskRegistration Run(string id, Func<CancellationToken, Task> task);

    IPluginTaskRegistration SchedulePeriodic(
        string id,
        TimeSpan interval,
        Func<CancellationToken, Task> task);
}

/// <summary>Read-only Minecraft instance listing (design §9; requires permission <c>pcl.instances.read</c>).</summary>
public sealed record PluginInstanceInfo(
    string Id,
    string Name,
    string InstanceDirectory,
    string? VersionJsonPath = null);

public interface IPluginInstanceReadService : IPluginService
{
    IReadOnlyList<PluginInstanceInfo> ListInstances();

    bool TryGetInstance(string id, out PluginInstanceInfo? instance);
}

/// <summary>
/// Matches host service versions against Manifest ranges such as <c>*</c>, <c>0.1</c>,
/// <c>&gt;=0.1</c>, or <c>&gt;=0.1 &lt;1.0</c> (space-separated constraints).
/// </summary>
public static class PluginServiceVersionRanges
{
    public static bool Matches(string versionRange, PluginApiVersion version) =>
        PluginApiVersionRange.TryParse(versionRange, out PluginApiVersionRange? range) &&
        range is not null &&
        range.Contains(version);
}

public sealed class PluginApiVersionRange
{
    private readonly IReadOnlyList<(string Operation, PluginApiVersion Version)> _constraints;

    private PluginApiVersionRange(string value, IReadOnlyList<(string, PluginApiVersion)> constraints)
    {
        Value = value;
        _constraints = constraints;
    }

    public string Value { get; }

    public static PluginApiVersionRange Parse(string value) =>
        TryParse(value, out PluginApiVersionRange? range) && range is not null
            ? range
            : throw new FormatException($"Invalid plugin API version range: {value}");

    public static bool TryParse(string? value, out PluginApiVersionRange? range)
    {
        range = null;
        if (string.IsNullOrWhiteSpace(value))
            return false;
        string normalized = value.Trim();
        if (normalized == "*")
        {
            range = new PluginApiVersionRange(normalized, []);
            return true;
        }

        List<(string, PluginApiVersion)> constraints = [];
        foreach (string token in normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string operation = token.StartsWith(">=", StringComparison.Ordinal) ? ">=" :
                token.StartsWith("<=", StringComparison.Ordinal) ? "<=" :
                token.StartsWith('>') ? ">" : token.StartsWith('<') ? "<" :
                token.StartsWith('=') ? "=" : "=";
            string versionText = operation == "=" && !token.StartsWith('=') ? token : token[operation.Length..];
            if (!PluginApiVersion.TryParse(versionText, out PluginApiVersion version))
                return false;
            constraints.Add((operation, version));
        }
        range = new PluginApiVersionRange(normalized, constraints);
        return constraints.Count > 0;
    }

    public bool Contains(PluginApiVersion version) => _constraints.All(constraint =>
    {
        int comparison = version.CompareTo(constraint.Version);
        return constraint.Operation switch
        {
            ">=" => comparison >= 0,
            ">" => comparison > 0,
            "<=" => comparison <= 0,
            "<" => comparison < 0,
            _ => comparison == 0
        };
    });

    public override string ToString() => Value;
}

/// <summary>Per-plugin filesystem layout under the host install root.</summary>
public sealed record PluginDirectorySet(
    string Root,
    string Data,
    string Cache,
    string Logs,
    string Temp)
{
    public static PluginDirectorySet CreateUnder(string pluginRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pluginRoot);
        string root = Path.GetFullPath(pluginRoot);
        return new PluginDirectorySet(
            root,
            Path.Combine(root, "data"),
            Path.Combine(root, "cache"),
            Path.Combine(root, "logs"),
            Path.Combine(root, "temp"));
    }

    public void EnsureCreated()
    {
        Directory.CreateDirectory(Root);
        Directory.CreateDirectory(Data);
        Directory.CreateDirectory(Cache);
        Directory.CreateDirectory(Logs);
        Directory.CreateDirectory(Temp);
    }
}
