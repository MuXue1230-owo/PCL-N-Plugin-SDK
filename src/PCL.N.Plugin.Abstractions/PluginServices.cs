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

    public static PluginServiceId GameSessions { get; } = new("pcl.game.sessions");

    public static PluginServiceId GameOutput { get; } = new("pcl.game.output");

    public static PluginServiceId LaunchEvents { get; } = new("pcl.launch.events");

    public static PluginServiceId Process { get; } = new("pcl.process");

    public static PluginServiceId Clipboard { get; } = new("pcl.clipboard");

    public static PluginServiceId Files { get; } = new("pcl.files");

    public static PluginServiceId AccountsRead { get; } = new("pcl.accounts.read");

    public static PluginServiceId Downloads { get; } = new("pcl.downloads");

    public static PluginServiceId LaunchModify { get; } = new("pcl.launch.modify");

    public static PluginServiceId Ui { get; } = new("pcl.ui");

    public static PluginServiceId UiPatch { get; } = new("pcl.ui.patch");

    /// <summary>Host-managed access to the PCL.N plugin market.</summary>
    public static PluginServiceId Market { get; } = new("pcl.market");

    public static PluginServiceId Localization { get; } = new("pcl.localization");

    public static PluginServiceId Exports { get; } = new("pcl.exports");

    /// <summary>Host-managed, per-plugin isolated operating-system credential storage.</summary>
    public static PluginServiceId SecureStorage { get; } = new("pcl.secure-storage");

    /// <summary>Host-mediated opener for external HTTP/HTTPS links.</summary>
    public static PluginServiceId UriLauncher { get; } = new("pcl.uri-launcher");

    /// <summary>
    /// Host task-manager progress surface (same UI as Minecraft install downloads).
    /// Use for long-running plugin downloads, installs, and updates.
    /// </summary>
    public static PluginServiceId BackgroundTasks { get; } = new("pcl.background-tasks");
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

    void LogError(string message, Exception? exception = null);
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

/// <summary>Typed key in a plugin-owned operating-system credential namespace.</summary>
public readonly record struct PluginSecretKey
{
    public PluginSecretKey(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128)
            throw new ArgumentException("Secret key must contain between 1 and 128 characters.", nameof(name));
        if (name.Any(static character => !(char.IsAsciiLetterOrDigit(character) || character is '.' or '-' or '_')))
            throw new ArgumentException("Secret key may contain only ASCII letters, digits, '.', '-' and '_'.", nameof(name));
        Name = name;
    }

    public string Name { get; }

    public override string ToString() => Name ?? string.Empty;
}

public enum PluginSecureStorageStatus
{
    Success,
    NotFound,
    Unavailable,
    QuotaExceeded,
    Failed
}

public sealed record PluginSecretReadResult(PluginSecureStorageStatus Status, byte[]? Value = null, string? Message = null);

public sealed record PluginSecretOperationResult(PluginSecureStorageStatus Status, string? Message = null)
{
    public bool IsSuccess => Status is PluginSecureStorageStatus.Success or PluginSecureStorageStatus.NotFound;
}

/// <summary>
/// Host-managed secure storage isolated to the calling plugin. Requires manifest permission
/// <c>secure-storage</c>. Implementations never fall back to plaintext persistence.
/// </summary>
public interface IPluginSecureStorage : IPluginService
{
    ValueTask<PluginSecretReadResult> ReadAsync(PluginSecretKey key, CancellationToken cancellationToken = default);

    ValueTask<PluginSecretOperationResult> WriteAsync(
        PluginSecretKey key,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default);

    ValueTask<PluginSecretOperationResult> DeleteAsync(PluginSecretKey key, CancellationToken cancellationToken = default);
}

/// <summary>
/// Host-mediated external URI launcher. Requires absolute HTTP/HTTPS URIs; Host implementations may
/// show confirmation UI or deny unsupported links.
/// </summary>
public interface IPluginUriLauncher : IPluginService
{
    ValueTask<bool> OpenAsync(Uri uri, CancellationToken cancellationToken = default);
}

/// <summary>State of one step inside a host-tracked background task.</summary>
public enum PluginBackgroundTaskStepState
{
    Waiting = 0,
    Running = 1,
    Finished = 2,
    Failed = 3
}

/// <summary>One pipeline step shown under a host task-manager card (MC install style).</summary>
public sealed record PluginBackgroundTaskStep(
    string Name,
    string Detail,
    double Progress,
    PluginBackgroundTaskStepState State);

/// <summary>Progress snapshot for <see cref="IPluginBackgroundTask.Report"/>.</summary>
public sealed record PluginBackgroundTaskProgress(
    string Stage,
    string Detail = "",
    double Progress = 0d,
    int CompletedFiles = 0,
    int TotalFiles = 0,
    long SpeedBytesPerSecond = 0,
    IReadOnlyList<PluginBackgroundTaskStep>? Steps = null);

/// <summary>
/// A single host-tracked background task (appears in the launcher task manager).
/// Dispose after Complete/Fail to unregister cancellation ownership.
/// </summary>
public interface IPluginBackgroundTask : IDisposable
{
    /// <summary>Cancelled when the user cancels the task in the host UI.</summary>
    CancellationToken Token { get; }

    void Report(PluginBackgroundTaskProgress progress);

    void Complete(string stage);

    void Fail(string message, bool canceled = false);
}

/// <summary>
/// Host task-manager bridge for plugin-owned downloads, installs, and updates.
/// Matches the Minecraft install download UI (stage, file counts, speed, cancellable).
/// No extra manifest permission is required; plugins only report progress for work they initiate.
/// </summary>
public interface IPluginBackgroundTaskService : IPluginService
{
    /// <param name="title">Task card title.</param>
    /// <param name="openTaskManager">When true, navigates to the host task manager page.</param>
    IPluginBackgroundTask Begin(string title, bool openTaskManager = true);
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

/// <summary>Reads plugin-owned localized strings from host-selected culture resources.</summary>
public interface IPluginLocalizationService : IPluginService
{
    string CurrentCulture { get; }

    string GetString(string key, string fallback);

    IReadOnlyDictionary<string, string> GetStrings();
}

/// <summary>Stable identifier of a service exported by another plugin.</summary>
public readonly record struct PluginExportId
{
    public PluginExportId(string pluginId, string name)
    {
        if (!global::PCL.N.Plugin.PluginId.TryParse(pluginId, out _))
            throw new ArgumentException("Export plugin ID is invalid.", nameof(pluginId));
        if (string.IsNullOrWhiteSpace(name) || name.Contains(':', StringComparison.Ordinal))
            throw new ArgumentException("Export name must be non-empty and cannot contain ':'.", nameof(name));
        PluginId = pluginId;
        Name = name.Trim();
    }

    public string PluginId { get; }

    public string Name { get; }

    public override string ToString() => PluginId + ":" + Name;
}

/// <summary>Metadata attached to a plugin-to-plugin exported service.</summary>
public sealed record PluginExportDescriptor
{
    public PluginExportDescriptor(string name, PluginApiVersion version)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Contains(':', StringComparison.Ordinal))
            throw new ArgumentException("Export name must be non-empty and cannot contain ':'.", nameof(name));
        Name = name.Trim();
        Version = version;
    }

    public string Name { get; init; }

    public PluginApiVersion Version { get; init; }
}

/// <summary>Result of importing a compatible shared contract from another plugin.</summary>
public sealed record PluginImport<TContract>(
    PluginExportId Id,
    PluginApiVersion Version,
    TContract? Value)
    where TContract : class
{
    public bool IsAvailable => Value is not null;

    public TContract Require() => Value ?? throw new InvalidOperationException($"Plugin export is unavailable: {Id}");
}

/// <summary>
/// Controlled plugin-to-plugin service exchange. Contract assemblies must be loaded in the
/// default context; plugin-private types are rejected by the runtime.
/// </summary>
public interface IPluginExportRegistry : IPluginService
{
    IPluginRegistration Export<TContract>(
        PluginExportDescriptor descriptor,
        TContract implementation)
        where TContract : class;

    PluginImport<TContract> Import<TContract>(
        PluginExportId id,
        PluginApiVersionRange version)
        where TContract : class;
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
