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

public sealed class TestPluginProcessService : IPluginProcessService
{
    private readonly Queue<PluginProcessResult> _queuedResults = new();
    public PluginServiceId Id => PluginServiceIds.Process;
    public PluginApiVersion Version { get; } = new(0, 1);
    public List<PluginProcessRequest> Requests { get; } = [];

    public TestPluginProcessService EnqueueResult(PluginProcessResult result)
    {
        _queuedResults.Enqueue(result);
        return this;
    }

    public Task<PluginProcessResult> RunAsync(PluginProcessRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();
        Requests.Add(request);
        return Task.FromResult(_queuedResults.Count > 0 ? _queuedResults.Dequeue() : new PluginProcessResult(0, string.Empty, string.Empty));
    }
}

public sealed class TestPluginClipboardService : IPluginClipboardService
{
    public PluginServiceId Id => PluginServiceIds.Clipboard;
    public PluginApiVersion Version { get; } = new(0, 1);
    public string? Text { get; private set; }
    public ValueTask<string?> ReadTextAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult(Text);
    }
    public ValueTask WriteTextAsync(string text, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        cancellationToken.ThrowIfCancellationRequested();
        Text = text;
        return ValueTask.CompletedTask;
    }
}

public sealed class TestPluginFileService : IPluginFileService
{
    private readonly string _root;
    public TestPluginFileService(PluginDirectorySet directories) => _root = directories.Data;
    public PluginServiceId Id => PluginServiceIds.Files;
    public PluginApiVersion Version { get; } = new(0, 1);

    public async ValueTask<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        string path = Resolve(relativePath);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken).ConfigureAwait(false) : null;
    }

    public async ValueTask WriteAsync(string relativePath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default)
    {
        string path = Resolve(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await File.WriteAllBytesAsync(path, content.ToArray(), cancellationToken).ConfigureAwait(false);
    }

    public ValueTask DeleteAsync(string relativePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        string path = Resolve(relativePath);
        if (File.Exists(path)) File.Delete(path);
        return ValueTask.CompletedTask;
    }

    public IReadOnlyList<string> List(string relativeDirectory = "")
    {
        string directory = ResolveDirectory(relativeDirectory);
        return Directory.Exists(directory)
            ? Directory.EnumerateFiles(directory).Select(path => Path.GetRelativePath(_root, path).Replace('\\', '/')).Order(StringComparer.Ordinal).ToArray()
            : [];
    }

    private string Resolve(string relativePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Path must be relative.", nameof(relativePath));
        string path = Path.GetFullPath(Path.Combine(_root, relativePath));
        EnsureContained(path);
        return path;
    }

    private string ResolveDirectory(string relativePath)
    {
        if (Path.IsPathRooted(relativePath)) throw new ArgumentException("Path must be relative.", nameof(relativePath));
        string path = Path.GetFullPath(Path.Combine(_root, relativePath));
        EnsureContained(path);
        return path;
    }

    private void EnsureContained(string path)
    {
        string normalizedRoot = Path.GetFullPath(_root).TrimEnd(Path.DirectorySeparatorChar);
        string fullRoot = normalizedRoot + Path.DirectorySeparatorChar;
        StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (!string.Equals(path, normalizedRoot, comparison) && !path.StartsWith(fullRoot, comparison))
            throw new UnauthorizedAccessException("Path escaped the test plugin data directory.");
    }
}

public sealed class TestPluginAccountReadService : IPluginAccountReadService
{
    private readonly List<PluginAccountProviderInfo> _providers = [];
    public PluginServiceId Id => PluginServiceIds.AccountsRead;
    public PluginApiVersion Version { get; } = new(0, 1);
    public TestPluginAccountReadService AddProvider(PluginAccountProviderInfo provider) { _providers.Add(provider); return this; }
    public IReadOnlyList<PluginAccountProviderInfo> ListProviders() => _providers.ToArray();
}

public sealed class TestPluginDownloadService : IPluginDownloadService
{
    private readonly List<PluginDownloadSourceInfo> _sources = [];
    public PluginServiceId Id => PluginServiceIds.Downloads;
    public PluginApiVersion Version { get; } = new(0, 1);
    public TestPluginDownloadService AddSource(PluginDownloadSourceInfo source) { _sources.Add(source); return this; }
    public IReadOnlyList<PluginDownloadSourceInfo> ListSources() => _sources.ToArray();
}

public sealed class TestPluginLaunchModificationService(TestPluginLifetime lifetime) : IPluginLaunchModificationService
{
    private readonly List<PluginLaunchModification> _modifications = [];
    public PluginServiceId Id => PluginServiceIds.LaunchModify;
    public PluginApiVersion Version { get; } = new(0, 1);
    public IReadOnlyList<PluginLaunchModification> Modifications => _modifications;

    public IPluginRegistration Register(PluginLaunchModification modification)
    {
        ArgumentNullException.ThrowIfNull(modification);
        _modifications.Add(modification);
        TestDelegateRegistration registration = new(modification.Id, () => _modifications.Remove(modification));
        lifetime.Track(registration);
        return registration;
    }

    public PluginLaunchRequest ApplyAll(PluginLaunchRequest request)
    {
        foreach (PluginLaunchModification modification in _modifications)
            request = modification.Apply(request);
        return request;
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
