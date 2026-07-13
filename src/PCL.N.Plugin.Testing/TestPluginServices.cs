using System.Collections.Concurrent;

namespace PCL.N.Plugin.Testing;

public sealed class TestPluginNotificationService : IPluginNotificationService
{
    private readonly ConcurrentQueue<string> _messages = new();
    public PluginServiceId Id => PluginServiceIds.Notifications;
    public PluginApiVersion Version => new(0, 1);
    public IReadOnlyCollection<string> Messages => _messages.ToArray();
    public void ShowInformation(string message) => _messages.Enqueue("info:" + message);
    public void ShowWarning(string message) => _messages.Enqueue("warning:" + message);
}

public sealed class TestPluginSettingsStore : IPluginSettingsStore
{
    private readonly ConcurrentDictionary<string, object?> _values = new(StringComparer.Ordinal);
    public PluginServiceId Id => PluginServiceIds.Settings;
    public PluginApiVersion Version => new(0, 1);

    public ValueTask<T> GetAsync<T>(PluginSettingKey<T> key, T defaultValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_values.TryGetValue(key.Name, out object? value) && value is T typed ? typed : defaultValue);
    }

    public ValueTask SetAsync<T>(PluginSettingKey<T> key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values[key.Name] = value;
        return ValueTask.CompletedTask;
    }
}

public sealed class TestPluginCommandService : IPluginCommandService
{
    private readonly Dictionary<string, PluginCommandDescriptor> _commands = new(StringComparer.OrdinalIgnoreCase);
    public PluginServiceId Id => PluginServiceIds.Commands;
    public PluginApiVersion Version => new(0, 1);
    public IReadOnlyCollection<PluginCommandDescriptor> Commands => _commands.Values;

    public IPluginRegistration Register(PluginCommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (!_commands.TryAdd(descriptor.Id, descriptor))
            throw new InvalidOperationException($"Command already registered: {descriptor.Id}");
        return new TestPluginRegistration(descriptor.Id, () => _commands.Remove(descriptor.Id));
    }

    public Task InvokeAsync(string commandId, CancellationToken cancellationToken = default) =>
        _commands.TryGetValue(commandId, out PluginCommandDescriptor? command)
            ? command.ExecuteAsync(cancellationToken)
            : throw new KeyNotFoundException($"Command not found: {commandId}");
}

public sealed class TestPluginInstanceReadService(IEnumerable<PluginInstanceInfo>? instances = null) : IPluginInstanceReadService
{
    private readonly IReadOnlyList<PluginInstanceInfo> _instances = instances?.ToArray() ?? [];
    public PluginServiceId Id => PluginServiceIds.InstancesRead;
    public PluginApiVersion Version => new(0, 1);
    public IReadOnlyList<PluginInstanceInfo> ListInstances() => _instances;
    public bool TryGetInstance(string id, out PluginInstanceInfo? instance)
    {
        instance = _instances.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        return instance is not null;
    }
}

public sealed class TestPluginRegistration(string id, Action release) : IPluginRegistration
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
