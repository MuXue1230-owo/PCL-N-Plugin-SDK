using System.Text.Json;

namespace PCL.N.Plugin;

/// <summary>Windows-style rights applied to one registry node and, optionally, its descendants.</summary>
[Flags]
public enum PluginRegistryRights
{
    None = 0,
    Read = 1 << 0,
    RegisterChildren = 1 << 1,
    OverrideValue = 1 << 2,
    Remove = 1 << 3,
    ManageAccess = 1 << 4,
    FullControl = Read | RegisterChildren | OverrideValue | Remove | ManageAccess
}

/// <param name="Principal">A plugin id or <c>*</c>.</param>
/// <param name="Rights">Rights granted or denied by this rule.</param>
/// <param name="Deny">Deny rules take precedence over allow rules.</param>
/// <param name="Inherit">Whether the rule applies to descendant nodes.</param>
public sealed record PluginRegistryAccessRule(
    string Principal,
    PluginRegistryRights Rights,
    bool Deny = false,
    bool Inherit = true);

/// <summary>An immutable JSON registry value; host objects never cross plugin load contexts.</summary>
public sealed record PluginRegistryNode(
    string Path,
    string OwnerPluginId,
    JsonElement Value,
    long Revision,
    bool IsProtected,
    IReadOnlyList<PluginRegistryAccessRule> AccessRules);

public sealed record PluginRegistryNodeDescriptor(
    string Path,
    JsonElement Value,
    IReadOnlyList<PluginRegistryAccessRule>? AccessRules = null);

public enum PluginRegistryChangeKind
{
    Registered,
    Updated,
    Overridden,
    Removed,
    AccessChanged
}

public sealed record PluginRegistryChange(
    PluginRegistryChangeKind Kind,
    PluginRegistryNode Node,
    string ChangedByPluginId);

/// <summary>A live owned registry node. Disposing it removes the node and every owned child registration.</summary>
public interface IPluginRegistryRegistration : IPluginRegistration
{
    long Revision { get; }

    void Update(JsonElement value);

    void SetAccessRules(IReadOnlyList<PluginRegistryAccessRule> rules);
}

/// <summary>
/// Composable host registry. Paths under <c>plugins.&lt;own-plugin-id&gt;</c> are owned by the caller.
/// Cross-plugin writes require both an effective permission and a matching ACL. Host protected paths
/// (including <c>pcl.plugin</c> and <c>pcl.security</c>) are never writable through this service.
/// </summary>
public interface IPluginRegistryService : IPluginService
{
    PluginRegistryNode? GetNode(string path);

    IReadOnlyList<PluginRegistryNode> ListNodes(string path, bool recursive = false);

    IPluginRegistryRegistration Register(PluginRegistryNodeDescriptor descriptor);

    /// <summary>Adds a reversible value layer without changing ownership of the target node.</summary>
    IPluginRegistration Override(string path, JsonElement value, int priority = 0);

    IPluginRegistration Watch(string path, Action<PluginRegistryChange> observer, bool recursive = true);
}
