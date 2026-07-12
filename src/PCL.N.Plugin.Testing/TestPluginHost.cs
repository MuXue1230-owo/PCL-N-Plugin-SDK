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

public sealed class TestPluginContext : IPluginContext, IAsyncDisposable
{
    public TestPluginContext(PluginDescriptor plugin, PluginApiVersion apiVersion)
    {
        ArgumentNullException.ThrowIfNull(plugin);
        Plugin = plugin;
        ApiVersion = apiVersion;
    }

    public PluginDescriptor Plugin { get; }

    public PluginApiVersion ApiVersion { get; }

    public TestPluginCapabilityProvider TestCapabilities { get; } = new();

    public TestPluginLifetime TestLifetime { get; } = new();

    public IPluginCapabilityProvider Capabilities => TestCapabilities;

    public IPluginLifetime Lifetime => TestLifetime;

    public CancellationToken Stopping => TestLifetime.Stopping;

    public ValueTask DisposeAsync() => TestLifetime.DisposeAsync();
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
