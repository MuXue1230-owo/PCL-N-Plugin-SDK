using System.Collections.Concurrent;
using System.Globalization;
using PCL.N.Plugin;

namespace PCL.N.Plugin.Testing;

public sealed class TestPluginNotificationService : IPluginNotificationService
{
    private readonly List<(string Level, string Message)> _messages = [];
    public PluginServiceId Id => PluginServiceIds.Notifications;
    public PluginApiVersion Version { get; } = new(0, 1);
    public IReadOnlyList<(string Level, string Message)> Messages => _messages;
    public void ShowInformation(string message) => _messages.Add(("information", message));
    public void ShowWarning(string message) => _messages.Add(("warning", message));
}

public sealed class TestPluginSettingsStore : IPluginSettingsStore
{
    private readonly ConcurrentDictionary<string, object?> _values = new(StringComparer.Ordinal);
    public PluginServiceId Id => PluginServiceIds.Settings;
    public PluginApiVersion Version { get; } = new(0, 1);

    public ValueTask<T> GetAsync<T>(PluginSettingKey<T> key, T defaultValue, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_values.TryGetValue(key.Name, out object? value) && value is T typed
            ? typed
            : defaultValue);
    }

    public ValueTask SetAsync<T>(PluginSettingKey<T> key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values[key.Name] = value;
        return ValueTask.CompletedTask;
    }
}

public sealed class TestPluginCommandService(TestPluginLifetime? lifetime = null) : IPluginCommandService
{
    private readonly ConcurrentDictionary<string, PluginCommandDescriptor> _commands =
        new(StringComparer.OrdinalIgnoreCase);
    public PluginServiceId Id => PluginServiceIds.Commands;
    public PluginApiVersion Version { get; } = new(0, 1);
    public IReadOnlyList<PluginCommandDescriptor> Commands => _commands.Values.ToArray();

    public IPluginRegistration Register(PluginCommandDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        if (!_commands.TryAdd(descriptor.Id, descriptor))
            throw new InvalidOperationException($"Command already registered: {descriptor.Id}");
        TestDelegateRegistration registration = new(descriptor.Id, () => _commands.TryRemove(descriptor.Id, out _));
        lifetime?.Track(registration);
        return registration;
    }

    public Task InvokeAsync(string commandId, CancellationToken cancellationToken = default) =>
        _commands.TryGetValue(commandId, out PluginCommandDescriptor? descriptor)
            ? descriptor.ExecuteAsync(cancellationToken)
            : Task.FromException(new KeyNotFoundException($"Command is not registered: {commandId}"));
}

public sealed class TestPluginInstanceReadService(
    IEnumerable<PluginInstanceInfo>? instances = null) : IPluginInstanceReadService
{
    private readonly PluginInstanceInfo[] _instances = instances?.ToArray() ?? [];
    public PluginServiceId Id => PluginServiceIds.InstancesRead;
    public PluginApiVersion Version { get; } = new(0, 1);
    public IReadOnlyList<PluginInstanceInfo> ListInstances() => _instances;
    public bool TryGetInstance(string id, out PluginInstanceInfo? instance)
    {
        instance = _instances.FirstOrDefault(item => string.Equals(item.Id, id, StringComparison.OrdinalIgnoreCase));
        return instance is not null;
    }
}

public sealed class TestPluginLocalizationService(
    IReadOnlyDictionary<string, string>? strings = null,
    string? culture = null) : IPluginLocalizationService
{
    private readonly IReadOnlyDictionary<string, string> _strings =
        strings ?? new Dictionary<string, string>();
    public PluginServiceId Id => PluginServiceIds.Localization;
    public PluginApiVersion Version { get; } = new(0, 1);
    public string CurrentCulture { get; } = culture ?? CultureInfo.CurrentUICulture.Name;
    public string GetString(string key, string fallback) => _strings.TryGetValue(key, out string? value) ? value : fallback;
    public IReadOnlyDictionary<string, string> GetStrings() => _strings;
}

public sealed class TestPluginSecureStorage : IPluginSecureStorage
{
    private readonly ConcurrentDictionary<string, byte[]> _values = new(StringComparer.Ordinal);

    public PluginServiceId Id => PluginServiceIds.SecureStorage;

    public PluginApiVersion Version { get; } = new(0, 1);

    public ValueTask<PluginSecretReadResult> ReadAsync(PluginSecretKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(_values.TryGetValue(key.Name, out byte[]? value)
            ? new PluginSecretReadResult(PluginSecureStorageStatus.Success, value.ToArray())
            : new PluginSecretReadResult(PluginSecureStorageStatus.NotFound));
    }

    public ValueTask<PluginSecretOperationResult> WriteAsync(
        PluginSecretKey key,
        ReadOnlyMemory<byte> value,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values[key.Name] = value.ToArray();
        return ValueTask.FromResult(new PluginSecretOperationResult(PluginSecureStorageStatus.Success));
    }

    public ValueTask<PluginSecretOperationResult> DeleteAsync(PluginSecretKey key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _values.TryRemove(key.Name, out _);
        return ValueTask.FromResult(new PluginSecretOperationResult(PluginSecureStorageStatus.Success));
    }
}

public sealed class TestPluginUriLauncher : IPluginUriLauncher
{
    private readonly List<Uri> _openedUris = [];

    public PluginServiceId Id => PluginServiceIds.UriLauncher;

    public PluginApiVersion Version { get; } = new(0, 1);

    public IReadOnlyList<Uri> OpenedUris => _openedUris;

    public ValueTask<bool> OpenAsync(Uri uri, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(uri);
        cancellationToken.ThrowIfCancellationRequested();
        if (!uri.IsAbsoluteUri || (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp))
            throw new ArgumentException("Only absolute HTTP and HTTPS URIs can be opened.", nameof(uri));
        _openedUris.Add(uri);
        return ValueTask.FromResult(true);
    }
}

public sealed class TestPluginExportRegistry(string pluginId, TestPluginLifetime lifetime) : IPluginExportRegistry
{
    private static readonly ConcurrentDictionary<(string PluginId, string Name, Type Contract), (PluginApiVersion Version, object Value)> Exports = new();
    public PluginServiceId Id => PluginServiceIds.Exports;
    public PluginApiVersion Version { get; } = new(0, 1);

    public IPluginRegistration Export<TContract>(PluginExportDescriptor descriptor, TContract implementation)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentNullException.ThrowIfNull(implementation);
        var key = (pluginId, descriptor.Name, typeof(TContract));
        var exported = (descriptor.Version, (object)implementation);
        if (!Exports.TryAdd(key, exported))
            throw new InvalidOperationException($"Export already registered: {pluginId}:{descriptor.Name}");
        TestDelegateRegistration registration = new(
            pluginId + ":" + descriptor.Name,
            () => Exports.TryRemove(new KeyValuePair<(string, string, Type), (PluginApiVersion, object)>(key, exported)));
        lifetime.Track(registration);
        return registration;
    }

    public PluginImport<TContract> Import<TContract>(PluginExportId id, PluginApiVersionRange version)
        where TContract : class
    {
        ArgumentNullException.ThrowIfNull(version);
        return Exports.TryGetValue((id.PluginId, id.Name, typeof(TContract)), out var exported) &&
               version.Contains(exported.Version)
            ? new PluginImport<TContract>(id, exported.Version, (TContract)exported.Value)
            : new PluginImport<TContract>(id, default, null);
    }
}

internal sealed class TestDelegateRegistration(string id, Action release) : IPluginRegistration
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
