namespace PCL.N.Plugin;

/// <summary>Kind of a host-declared UI surface (design §11).</summary>
public enum PluginUiSurfaceKind
{
    Page,
    Window,
    Dialog,
    Overlay,
    Menu,
    Control,
    Region
}

/// <summary>Operations a surface or slot may allow (design §11).</summary>
public enum PluginUiOperation
{
    Observe,
    Inject,
    Modify,
    Replace,
    Wrap
}

/// <summary>How many contributions a slot accepts.</summary>
public enum PluginUiSlotCardinality
{
    One,
    Many
}

/// <summary>Stable slot within a surface (e.g. <c>cards.flip</c> or legacy <c>primary-actions.after</c>).</summary>
public sealed record PluginUiSlotDescriptor
{
    public PluginUiSlotDescriptor(
        string id,
        PluginUiSlotCardinality cardinality,
        IReadOnlyList<PluginUiOperation> allowedOperations)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentNullException.ThrowIfNull(allowedOperations);
        Id = id.Trim();
        Cardinality = cardinality;
        AllowedOperations = allowedOperations;
    }

    public string Id { get; init; }

    public PluginUiSlotCardinality Cardinality { get; init; }

    public IReadOnlyList<PluginUiOperation> AllowedOperations { get; init; }
}

/// <summary>Host-published UI surface that plugins may target.</summary>
public sealed record PluginUiSurfaceDescriptor
{
    public PluginUiSurfaceDescriptor(
        string id,
        PluginUiSurfaceKind kind,
        string version,
        IReadOnlyList<PluginUiOperation> supportedOperations,
        IReadOnlyList<PluginUiSlotDescriptor> slots)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(version);
        ArgumentNullException.ThrowIfNull(supportedOperations);
        ArgumentNullException.ThrowIfNull(slots);
        Id = id.Trim();
        Kind = kind;
        Version = version.Trim();
        SupportedOperations = supportedOperations;
        Slots = slots;
    }

    public string Id { get; init; }

    public PluginUiSurfaceKind Kind { get; init; }

    public string Version { get; init; }

    public IReadOnlyList<PluginUiOperation> SupportedOperations { get; init; }

    public IReadOnlyList<PluginUiSlotDescriptor> Slots { get; init; }

    public bool TryGetSlot(string slotId, out PluginUiSlotDescriptor? slot)
    {
        foreach (PluginUiSlotDescriptor candidate in Slots)
        {
            if (string.Equals(candidate.Id, slotId, StringComparison.OrdinalIgnoreCase))
            {
                slot = candidate;
                return true;
            }
        }

        slot = null;
        return false;
    }
}

/// <summary>
/// Read-only registry of host UI surfaces (design §11.1).
/// Patch application is a later phase; this surface only publishes targets and validates operations.
/// </summary>
public interface IPluginUiSurfaceRegistry : IPluginService
{
    IReadOnlyList<PluginUiSurfaceDescriptor> ListSurfaces();

    bool TryGetSurface(string surfaceId, out PluginUiSurfaceDescriptor? surface);

    /// <summary>
    /// Returns true when the host publishes <paramref name="surfaceId"/> at a version matching
    /// <paramref name="versionRange"/> and allows <paramref name="operation"/>.
    /// </summary>
    bool Supports(string surfaceId, string versionRange, PluginUiOperation operation);

    /// <summary>
    /// Returns true when the named slot exists and allows <paramref name="operation"/>.
    /// </summary>
    bool SupportsSlot(
        string surfaceId,
        string slotId,
        string versionRange,
        PluginUiOperation operation);
}

/// <summary>
/// Declares intent to inject into a host slot. The host records the registration;
/// visual composition / Patch engine is deferred (design §12).
/// </summary>
public sealed record PluginUiSlotContributionDescriptor
{
    public PluginUiSlotContributionDescriptor(
        string surfaceId,
        string slotId,
        string contributionId,
        int order = 0,
        string? title = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(slotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(contributionId);
        SurfaceId = surfaceId.Trim();
        SlotId = slotId.Trim();
        ContributionId = contributionId.Trim();
        Order = order;
        Title = title;
    }

    public string SurfaceId { get; init; }

    public string SlotId { get; init; }

    public string ContributionId { get; init; }

    public int Order { get; init; }

    public string? Title { get; init; }
}

public interface IPluginUiSurfaceCapability : IPluginCapability
{
    /// <summary>Records a slot contribution; released automatically when the plugin unloads.</summary>
    IPluginRegistration Contribute(PluginUiSlotContributionDescriptor descriptor);
}
