namespace PCL.N.Plugin;

/// <summary>Stable service identifier (e.g. <c>pcl.settings</c>).</summary>
public readonly record struct PluginServiceId
{
    public PluginServiceId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Service ID cannot be empty.", nameof(value));
        Value = value.Trim();
    }

    public string Value { get; }

    public override string ToString() => Value;

    public static implicit operator string(PluginServiceId id) => id.Value;
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

    bool Supports(PluginServiceId serviceId, string versionRange);
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
