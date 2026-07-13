namespace PCL.N.Plugin.Testing;

public sealed class TestPluginTaskService : IPluginTaskService, IAsyncDisposable
{
    private readonly List<TestPluginTaskRegistration> _tasks = [];
    public PluginServiceId Id => PluginServiceIds.Tasks;
    public PluginApiVersion Version => new(0, 1);
    public IReadOnlyList<IPluginTaskRegistration> Tasks => _tasks;

    public IPluginTaskRegistration Run(string id, Func<CancellationToken, Task> task) => Add(id, task, null);

    public IPluginTaskRegistration SchedulePeriodic(string id, TimeSpan interval, Func<CancellationToken, Task> task)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(interval, TimeSpan.Zero);
        return Add(id, task, interval);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (TestPluginTaskRegistration task in _tasks.ToArray()) await task.DisposeAsync().ConfigureAwait(false);
        _tasks.Clear();
    }

    private TestPluginTaskRegistration Add(string id, Func<CancellationToken, Task> action, TimeSpan? interval)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(action);
        if (_tasks.Any(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException($"Task already registered: {id}");
        TestPluginTaskRegistration registration = new(id, action, interval, () =>
            _tasks.RemoveAll(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase)));
        _tasks.Add(registration);
        return registration;
    }
}

internal sealed class TestPluginTaskRegistration : IPluginTaskRegistration
{
    private readonly CancellationTokenSource _stopping = new();
    private readonly Action _release;
    private int _disposed;

    public TestPluginTaskRegistration(string id, Func<CancellationToken, Task> action, TimeSpan? interval, Action release)
    {
        Id = id;
        _release = release;
        Completion = interval is null ? action(_stopping.Token) : RunPeriodicAsync(action, interval.Value, _stopping.Token);
    }

    public string Id { get; }
    public bool IsActive => Volatile.Read(ref _disposed) == 0;
    public Task Completion { get; }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        await _stopping.CancelAsync().ConfigureAwait(false);
        try { await Completion.ConfigureAwait(false); } catch (OperationCanceledException) when (_stopping.IsCancellationRequested) { }
        _release();
        _stopping.Dispose();
    }

    private static async Task RunPeriodicAsync(Func<CancellationToken, Task> action, TimeSpan interval, CancellationToken token)
    {
        using PeriodicTimer timer = new(interval);
        while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false)) await action(token).ConfigureAwait(false);
    }
}

public sealed class TestPluginUiSurfaceRegistry(IEnumerable<PluginUiSurfaceDescriptor>? surfaces = null) : IPluginUiSurfaceRegistry
{
    private readonly IReadOnlyList<PluginUiSurfaceDescriptor> _surfaces = surfaces?.ToArray() ?? [];
    public PluginServiceId Id => PluginServiceIds.Ui;
    public PluginApiVersion Version => new(0, 1);
    public IReadOnlyList<PluginUiSurfaceDescriptor> ListSurfaces() => _surfaces;
    public bool TryGetSurface(string surfaceId, out PluginUiSurfaceDescriptor? surface)
    {
        surface = _surfaces.FirstOrDefault(item => string.Equals(item.Id, surfaceId, StringComparison.OrdinalIgnoreCase));
        return surface is not null;
    }
    public bool Supports(string surfaceId, string versionRange, PluginUiOperation operation) =>
        TryGetSurface(surfaceId, out PluginUiSurfaceDescriptor? surface) && surface!.SupportedOperations.Contains(operation);
    public bool SupportsSlot(string surfaceId, string slotId, string versionRange, PluginUiOperation operation) =>
        TryGetSurface(surfaceId, out PluginUiSurfaceDescriptor? surface) && surface!.TryGetSlot(slotId, out PluginUiSlotDescriptor? slot) && slot!.AllowedOperations.Contains(operation);
}
