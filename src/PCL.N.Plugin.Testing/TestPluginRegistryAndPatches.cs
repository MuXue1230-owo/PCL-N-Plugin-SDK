using System.Reflection;
using System.Text.Json;

namespace PCL.N.Plugin.Testing;

public sealed class TestPluginRegistryService(
    string pluginId,
    TestPluginLifetime? lifetime = null) : IPluginRegistryService
{
    private static readonly string[] ProtectedPrefixes = ["pcl.plugin", "pcl.security"];
    private readonly Dictionary<string, Entry> _entries = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Watcher> _watchers = [];
    private long _revision;

    public PluginServiceId Id => PluginServiceIds.Registry;

    public PluginApiVersion Version { get; } = new(0, 1);

    public PluginRegistryNode? GetNode(string path) =>
        _entries.TryGetValue(Normalize(path), out Entry? entry) ? entry.Snapshot() : null;

    public IReadOnlyList<PluginRegistryNode> ListNodes(string path, bool recursive = false)
    {
        string root = Normalize(path);
        return _entries.Values
            .Where(entry => recursive ? IsAtOrBelow(entry.Path, root) : IsDirectChild(entry.Path, root))
            .OrderBy(static entry => entry.Path, StringComparer.OrdinalIgnoreCase)
            .Select(static entry => entry.Snapshot())
            .ToArray();
    }

    public IPluginRegistryRegistration Register(PluginRegistryNodeDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        string path = Normalize(descriptor.Path);
        EnsureNotProtected(path);
        string ownRoot = "plugins." + pluginId;
        if (!IsAtOrBelow(path, ownRoot))
            throw new UnauthorizedAccessException($"Test registry only permits owned registration below {ownRoot}.");
        if (_entries.ContainsKey(path))
            throw new InvalidOperationException($"Registry node already exists: {path}");
        Entry entry = new(path, pluginId, descriptor.Value.Clone(), ++_revision, descriptor.AccessRules ?? []);
        _entries.Add(path, entry);
        Notify(PluginRegistryChangeKind.Registered, entry);
        Registration registration = new(this, entry);
        lifetime?.Track(registration);
        return registration;
    }

    /// <summary>Seeds another owner in tests so ACL and cross-plugin composition can be exercised.</summary>
    public void Seed(string ownerPluginId, PluginRegistryNodeDescriptor descriptor)
    {
        string path = Normalize(descriptor.Path);
        EnsureNotProtected(path);
        _entries.Add(path, new Entry(path, ownerPluginId, descriptor.Value.Clone(), ++_revision, descriptor.AccessRules ?? []));
    }

    public IPluginRegistration Override(string path, JsonElement value, int priority = 0)
    {
        path = Normalize(path);
        EnsureNotProtected(path);
        if (!_entries.TryGetValue(path, out Entry? entry))
            throw new KeyNotFoundException($"Registry node is not registered: {path}");
        if (!string.Equals(entry.Owner, pluginId, StringComparison.OrdinalIgnoreCase) &&
            !HasRight(entry, pluginId, PluginRegistryRights.OverrideValue))
        {
            throw new UnauthorizedAccessException($"Registry ACL denies OverrideValue: {path}");
        }
        OverrideLayer layer = new(pluginId, value.Clone(), priority, ++_revision);
        entry.Overrides.Add(layer);
        entry.Revision = layer.Revision;
        Notify(PluginRegistryChangeKind.Overridden, entry);
        TestRegistration registration = new($"registry-override:{path}:{layer.Revision}", () =>
        {
            entry.Overrides.Remove(layer);
            entry.Revision = ++_revision;
            Notify(PluginRegistryChangeKind.Updated, entry);
        });
        lifetime?.Track(registration);
        return registration;
    }

    public IPluginRegistration Watch(string path, Action<PluginRegistryChange> observer, bool recursive = true)
    {
        ArgumentNullException.ThrowIfNull(observer);
        Watcher watcher = new(Normalize(path), recursive, observer);
        _watchers.Add(watcher);
        TestRegistration registration = new($"registry-watch:{watcher.Path}", () => _watchers.Remove(watcher));
        lifetime?.Track(registration);
        return registration;
    }

    private void Notify(PluginRegistryChangeKind kind, Entry entry)
    {
        PluginRegistryChange change = new(kind, entry.Snapshot(), pluginId);
        foreach (Watcher watcher in _watchers.ToArray())
        {
            if (watcher.Recursive ? IsAtOrBelow(entry.Path, watcher.Path) :
                string.Equals(entry.Path, watcher.Path, StringComparison.OrdinalIgnoreCase))
            {
                watcher.Observer(change);
            }
        }
    }

    private static bool HasRight(Entry entry, string principal, PluginRegistryRights right)
    {
        IEnumerable<PluginRegistryAccessRule> rules = entry.Rules.Where(rule =>
            rule.Principal == "*" || string.Equals(rule.Principal, principal, StringComparison.OrdinalIgnoreCase));
        if (rules.Any(rule => rule.Deny && (rule.Rights & right) == right))
            return false;
        return rules.Any(rule => !rule.Deny && (rule.Rights & right) == right);
    }

    private static string Normalize(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        string value = path.Trim().Trim('.');
        if (value.Split('.').Any(static segment => segment.Length == 0))
            throw new ArgumentException("Registry paths cannot contain empty segments.", nameof(path));
        return value;
    }

    private static void EnsureNotProtected(string path)
    {
        if (ProtectedPrefixes.Any(prefix => IsAtOrBelow(path, prefix)))
            throw new UnauthorizedAccessException($"Registry path is permanently protected: {path}");
    }

    private static bool IsAtOrBelow(string path, string root) =>
        string.Equals(path, root, StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith(root + ".", StringComparison.OrdinalIgnoreCase);

    private static bool IsDirectChild(string path, string root)
    {
        if (!path.StartsWith(root + ".", StringComparison.OrdinalIgnoreCase))
            return false;
        return path[(root.Length + 1)..].IndexOf('.') < 0;
    }

    private sealed class Entry(
        string path,
        string owner,
        JsonElement value,
        long revision,
        IReadOnlyList<PluginRegistryAccessRule> rules)
    {
        public string Path { get; } = path;
        public string Owner { get; } = owner;
        public JsonElement Value { get; set; } = value;
        public long Revision { get; set; } = revision;
        public IReadOnlyList<PluginRegistryAccessRule> Rules { get; set; } = rules.ToArray();
        public List<OverrideLayer> Overrides { get; } = [];
        public PluginRegistryNode Snapshot()
        {
            OverrideLayer? layer = Overrides.OrderByDescending(static item => item.Priority)
                .ThenByDescending(static item => item.Revision).FirstOrDefault();
            return new PluginRegistryNode(
                Path,
                Owner,
                (layer?.Value ?? Value).Clone(),
                Revision,
                IsProtected: false,
                Rules.ToArray());
        }
    }

    private sealed record OverrideLayer(string Owner, JsonElement Value, int Priority, long Revision);
    private sealed record Watcher(string Path, bool Recursive, Action<PluginRegistryChange> Observer);

    private sealed class Registration(TestPluginRegistryService owner, Entry entry) : IPluginRegistryRegistration
    {
        private bool _active = true;
        public string Id => "registry:" + entry.Path;
        public bool IsActive => _active;
        public long Revision => entry.Revision;
        public void Update(JsonElement value)
        {
            EnsureActive();
            entry.Value = value.Clone();
            entry.Revision = ++owner._revision;
            owner.Notify(PluginRegistryChangeKind.Updated, entry);
        }
        public void SetAccessRules(IReadOnlyList<PluginRegistryAccessRule> rules)
        {
            EnsureActive();
            ArgumentNullException.ThrowIfNull(rules);
            entry.Rules = rules.ToArray();
            entry.Revision = ++owner._revision;
            owner.Notify(PluginRegistryChangeKind.AccessChanged, entry);
        }
        public ValueTask DisposeAsync()
        {
            if (_active)
            {
                _active = false;
                owner._entries.Remove(entry.Path);
                entry.Revision = ++owner._revision;
                owner.Notify(PluginRegistryChangeKind.Removed, entry);
            }
            return ValueTask.CompletedTask;
        }
        private void EnsureActive()
        {
            ObjectDisposedException.ThrowIf(!_active, this);
        }
    }

    private sealed class TestRegistration(string id, Action release) : IPluginRegistration
    {
        public string Id { get; } = id;
        public bool IsActive { get; private set; } = true;
        public ValueTask DisposeAsync()
        {
            if (IsActive) { IsActive = false; release(); }
            return ValueTask.CompletedTask;
        }
    }
}

public sealed class TestPluginRuntimePatchService(
    string pluginId,
    TestPluginLifetime? lifetime = null) : IPluginRuntimePatchService
{
    private readonly Dictionary<string, PluginRuntimePatchInfo> _patches = new(StringComparer.OrdinalIgnoreCase);
    public PluginServiceId Id => PluginServiceIds.RuntimePatches;
    public PluginApiVersion Version { get; } = new(0, 1);
    public IPluginRegistration Register(PluginRuntimePatchDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        ArgumentException.ThrowIfNullOrWhiteSpace(descriptor.PatchId);
        if (string.Equals(descriptor.Target.AssemblyName, "PCL.Plugin", StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("PCL.Plugin runtime targets are permanently protected.");
        if (descriptor.Prefix is null && descriptor.Postfix is null && descriptor.Transpiler is null && descriptor.Finalizer is null)
            throw new ArgumentException("At least one patch method is required.", nameof(descriptor));
        foreach (MethodInfo method in new[] { descriptor.Prefix, descriptor.Postfix, descriptor.Transpiler, descriptor.Finalizer }.OfType<MethodInfo>())
        {
            if (!method.IsStatic)
                throw new ArgumentException("Runtime patch methods must be static.", nameof(descriptor));
        }
        string globalId = pluginId + ":" + descriptor.PatchId;
        PluginRuntimePatchInfo info = new(
            globalId,
            descriptor.Target,
            descriptor.Prefix is not null,
            descriptor.Postfix is not null,
            descriptor.Transpiler is not null,
            descriptor.Finalizer is not null,
            descriptor.Priority);
        if (!_patches.TryAdd(globalId, info))
            throw new InvalidOperationException($"Runtime patch already registered: {globalId}");
        TestRegistration registration = new(globalId, () => _patches.Remove(globalId));
        lifetime?.Track(registration);
        return registration;
    }
    public IReadOnlyList<PluginRuntimePatchInfo> ListOwned() => _patches.Values.ToArray();
    private sealed class TestRegistration(string id, Action release) : IPluginRegistration
    {
        public string Id { get; } = id;
        public bool IsActive { get; private set; } = true;
        public ValueTask DisposeAsync()
        {
            if (IsActive) { IsActive = false; release(); }
            return ValueTask.CompletedTask;
        }
    }
}
