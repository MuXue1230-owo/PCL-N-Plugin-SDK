namespace PCL.N.Plugin.Testing;

public sealed class TestPluginCapabilityProvider : IPluginCapabilityProvider
{
    private readonly Dictionary<Type, IPluginCapability> _capabilities = [];

    public TestPluginCapabilityProvider Add<TCapability>(TCapability capability)
        where TCapability : class, IPluginCapability
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (!_capabilities.TryAdd(typeof(TCapability), capability))
            throw new InvalidOperationException($"Capability already registered: {typeof(TCapability).FullName}");
        return this;
    }

    public bool TryGet<TCapability>(out TCapability? capability)
        where TCapability : class, IPluginCapability
    {
        if (_capabilities.TryGetValue(typeof(TCapability), out IPluginCapability? value))
        {
            capability = (TCapability)value;
            return true;
        }

        capability = null;
        return false;
    }
}

public sealed class TestPluginLifetime : IPluginLifetime, IAsyncDisposable
{
    private readonly List<object> _tracked = [];
    private readonly CancellationTokenSource _stopping = new();
    private int _disposed;

    public CancellationToken Stopping => _stopping.Token;

    public void Track(IPluginRegistration registration) => TrackObject(registration);

    public void Track(IDisposable disposable) => TrackObject(disposable);

    public void Track(IAsyncDisposable disposable) => TrackObject(disposable);

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        await _stopping.CancelAsync().ConfigureAwait(false);
        for (int index = _tracked.Count - 1; index >= 0; index--)
        {
            switch (_tracked[index])
            {
                case IAsyncDisposable asyncDisposable:
                    await asyncDisposable.DisposeAsync().ConfigureAwait(false);
                    break;
                case IDisposable disposable:
                    disposable.Dispose();
                    break;
            }
        }

        _tracked.Clear();
        _stopping.Dispose();
    }

    private void TrackObject(object value)
    {
        ArgumentNullException.ThrowIfNull(value);
        if (!_tracked.Contains(value))
            _tracked.Add(value);
    }
}

public sealed class TestPluginServiceProvider : IPluginServiceProvider
{
    private readonly Dictionary<Type, IPluginService> _services = [];
    private readonly Dictionary<string, IPluginService> _byId = new(StringComparer.OrdinalIgnoreCase);

    public TestPluginServiceProvider Add<TService>(TService service)
        where TService : class, IPluginService
    {
        ArgumentNullException.ThrowIfNull(service);
        if (!_services.TryAdd(typeof(TService), service))
            throw new InvalidOperationException($"Service already registered: {typeof(TService).FullName}");
        _byId[service.Id.Value] = service;
        return this;
    }

    public bool TryGet<TService>(out TService? service)
        where TService : class, IPluginService
    {
        if (_services.TryGetValue(typeof(TService), out IPluginService? value))
        {
            service = (TService)value;
            return true;
        }

        service = null;
        return false;
    }

    public TService Require<TService>()
        where TService : class, IPluginService
    {
        if (TryGet(out TService? service) && service is not null)
            return service;
        throw new NotSupportedException($"Service not available: {typeof(TService).FullName}");
    }

    public bool Supports(PluginServiceId serviceId, PluginApiVersionRange versionRange)
    {
        ArgumentNullException.ThrowIfNull(versionRange);
        if (!_byId.TryGetValue(serviceId.Value, out IPluginService? service))
            return false;
        return versionRange.Contains(service.Version);
    }
}

public sealed class NullPluginLogger : IPluginLogger
{
    public static NullPluginLogger Instance { get; } = new();

    public void Trace(string message) { }

    public void Debug(string message) { }

    public void Info(string message) { }

    public void Warn(string message) { }

    public void Error(string message, Exception? exception = null) { }
}

public sealed class ImmediatePluginDispatcher : IPluginDispatcher
{
    public static ImmediatePluginDispatcher Instance { get; } = new();

    public void Post(Action action)
    {
        ArgumentNullException.ThrowIfNull(action);
        action();
    }

    public Task InvokeAsync(Action action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        action();
        return Task.CompletedTask;
    }

    public Task<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(action());
    }
}

public sealed class TestPluginContext : IPluginContext, IAsyncDisposable
{
    public TestPluginContext(
        PluginDescriptor plugin,
        PluginApiVersion apiVersion,
        PluginDirectorySet? directories = null)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        Plugin = plugin;
        ApiVersion = apiVersion;
        string root = directories?.Root
            ?? Path.Combine(Path.GetTempPath(), "pcl-n-plugin-test", plugin.Id.Value);
        Directories = directories ?? PluginDirectorySet.CreateUnder(root);
        Directories.EnsureCreated();
        Services = TestServices;
        Logger = new CollectingPluginLogger();
        Dispatcher = ImmediatePluginDispatcher.Instance;
    }

    public PluginDescriptor Plugin { get; }

    public PluginApiVersion ApiVersion { get; }

    public TestPluginServiceProvider TestServices { get; } = new();

    public TestPluginCapabilityProvider TestCapabilities { get; } = new();

    public TestPluginLifetime TestLifetime { get; } = new();

    public CollectingPluginLogger Logger { get; }

    public IPluginServiceProvider Services { get; }

    public IPluginCapabilityProvider Capabilities => TestCapabilities;

    public IPluginLifetime Lifetime => TestLifetime;

    public IPluginDispatcher Dispatcher { get; }

    public PluginDirectorySet Directories { get; }

    public CancellationToken Stopping => TestLifetime.Stopping;

    IPluginLogger IPluginContext.Logger => Logger;

    public ValueTask DisposeAsync() => TestLifetime.DisposeAsync();
}

public sealed class CollectingPluginLogger : IPluginLogger
{
    private readonly List<string> _entries = [];

    public IReadOnlyList<string> Entries => _entries;

    public void Trace(string message) => Add("trace", message, null);

    public void Debug(string message) => Add("debug", message, null);

    public void Info(string message) => Add("info", message, null);

    public void Warn(string message) => Add("warn", message, null);

    public void LogError(string message, Exception? exception = null) => Add("error", message, exception);

    private void Add(string level, string message, Exception? exception)
    {
        string line = exception is null
            ? $"[{level}] {message}"
            : $"[{level}] {message} :: {exception.GetType().Name}: {exception.Message}";
        _entries.Add(line);
    }
}

public sealed class InMemoryPluginSettingsPageCapability : IPluginSettingsPageCapability
{
    private readonly List<PluginSettingsPageDescriptor> _pages = [];
    private readonly HashSet<string> _ids = new(StringComparer.OrdinalIgnoreCase);

    public string Id => "pcl.settings-pages";

    public PluginApiVersion Version => new(0, 1);

    public IReadOnlyList<PluginSettingsPageDescriptor> Pages => _pages;

    public IPluginRegistration Register(PluginSettingsPageDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.Title);
        if (!_ids.Add(descriptor.Id))
            throw new InvalidOperationException($"Settings page already registered: {descriptor.Id}");

        _pages.Add(descriptor);
        return new DelegatePluginRegistration(descriptor.Id, () =>
        {
            _ids.Remove(descriptor.Id);
            _pages.Remove(descriptor);
        });
    }

    private sealed class DelegatePluginRegistration(string id, Action release) : IPluginRegistration
    {
        private Action? _release = release;

        public string Id { get; } = id;

        public bool IsActive => _release is not null;

        public ValueTask DisposeAsync()
        {
            Interlocked.Exchange(ref _release, null)?.Invoke();
            return ValueTask.CompletedTask;
        }
    }
}
