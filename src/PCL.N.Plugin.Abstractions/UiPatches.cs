namespace PCL.N.Plugin;

/// <summary>UI patch operation kinds (design §12.1).</summary>
public enum PluginUiPatchKind
{
    Observe,
    Register,
    Inject,
    Modify,
    Replace,
    Remove,
    Reorder,
    OverrideResource,
    OverrideStyle,
    OverrideTemplate,
    InterceptInput,
    Wrap
}

/// <summary>How competing Modify operations on the same property resolve (design §12.4).</summary>
public enum PluginUiModifyConflictPolicy
{
    Exclusive,
    FirstWriter,
    LastWriter,
    Merge,
    Chain,
    Custom
}

/// <summary>Fallback when a required patch cannot be applied (design §12.2).</summary>
public enum PluginUiPatchFallback
{
    DisableFeature,
    SkipPatch,
    FailLoad
}

/// <summary>Declared UI patch from a plugin (global id is <c>pluginId:operationId</c>).</summary>
public sealed record PluginUiPatchDescriptor
{
    public PluginUiPatchDescriptor(
        string operationId,
        string target,
        PluginUiPatchKind kind,
        string surfaceVersionRange = "*",
        string? slot = null,
        int priority = 0,
        bool required = false,
        PluginUiPatchFallback fallback = PluginUiPatchFallback.SkipPatch,
        IReadOnlyList<string>? before = null,
        IReadOnlyList<string>? after = null,
        string? propertyPath = null,
        PluginUiModifyConflictPolicy modifyPolicy = PluginUiModifyConflictPolicy.Exclusive,
        bool allowWrapping = false,
        object? value = null,
        PluginUiPatchPreconditions? preconditions = null,
        string? selector = null,
        string? resourcePath = null,
        string? commandId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(target);
        ArgumentException.ThrowIfNullOrWhiteSpace(surfaceVersionRange);
        OperationId = operationId.Trim();
        Target = target.Trim();
        Kind = kind;
        SurfaceVersionRange = surfaceVersionRange.Trim();
        Slot = string.IsNullOrWhiteSpace(slot) ? null : slot.Trim();
        Priority = priority;
        Required = required;
        Fallback = fallback;
        Before = before ?? [];
        After = after ?? [];
        PropertyPath = string.IsNullOrWhiteSpace(propertyPath) ? null : propertyPath.Trim();
        ModifyPolicy = modifyPolicy;
        AllowWrapping = allowWrapping;
        Value = value;
        Preconditions = preconditions;
        Selector = string.IsNullOrWhiteSpace(selector) ? null : selector.Trim();
        ResourcePath = string.IsNullOrWhiteSpace(resourcePath) ? null : resourcePath.Trim();
        CommandId = string.IsNullOrWhiteSpace(commandId) ? null : commandId.Trim();
    }

    public string OperationId { get; init; }

    public string Target { get; init; }

    public PluginUiPatchKind Kind { get; init; }

    public string SurfaceVersionRange { get; init; }

    public string? Slot { get; init; }

    public int Priority { get; init; }

    public bool Required { get; init; }

    public PluginUiPatchFallback Fallback { get; init; }

    /// <summary>Global patch ids or patterns (<c>plugin:*</c>) that must run after this patch.</summary>
    public IReadOnlyList<string> Before { get; init; }

    /// <summary>Global patch ids or patterns that must run before this patch.</summary>
    public IReadOnlyList<string> After { get; init; }

    /// <summary>For Modify: property path under the target (e.g. <c>Title</c>).</summary>
    public string? PropertyPath { get; init; }

    public PluginUiModifyConflictPolicy ModifyPolicy { get; init; }

    /// <summary>When Replace is true, wrappers may still apply if peers declare wrap.</summary>
    public bool AllowWrapping { get; init; }

    /// <summary>Typed value assigned by a Modify operation.</summary>
    public object? Value { get; init; }

    public PluginUiPatchPreconditions? Preconditions { get; init; }

    /// <summary>Optional stable component target inside the declared surface.</summary>
    public string? Selector { get; init; }

    /// <summary>Package-relative declarative AXAML content for register/inject/replace/wrap.</summary>
    public string? ResourcePath { get; init; }

    /// <summary>Optional command exposed to declarative content through its Commands binding map.</summary>
    public string? CommandId { get; init; }
}

public sealed record PluginUiPatchPreconditions
{
    public string? SurfaceVersionRange { get; init; }

    public IReadOnlyList<string> RequiredSlots { get; init; } = [];

    public IReadOnlyList<string> RequiredProperties { get; init; } = [];

    public IReadOnlyList<string> RequiredEvents { get; init; } = [];
}

public enum PluginUiConflictSeverity
{
    Compatible,
    Warning,
    Error
}

public enum PluginUiConflictKind
{
    None,
    ReplaceExclusive,
    HighRiskPair,
    ModifyPropertyExclusive,
    OrderingCycle,
    SurfaceUnsupported,
    SlotUnsupported
    ,MutualCompatibilityRequired
    ,UiPreconditionFailed
}

public sealed record PluginUiConflict(
    PluginUiConflictKind Kind,
    PluginUiConflictSeverity Severity,
    string LeftGlobalId,
    string RightGlobalId,
    string Target,
    string Message);

public sealed record PluginUiPatchPlan(
    IReadOnlyList<string> OrderedGlobalIds,
    IReadOnlyList<PluginUiConflict> Conflicts,
    bool HasBlockingConflicts)
{
    public static PluginUiPatchPlan Empty { get; } = new([], [], false);
}

/// <summary>
/// Host patch coordinator: records patches, plans order, reports conflicts (design §12–§13).
/// Visual Avalonia application is deferred.
/// </summary>
public interface IPluginUiPatchService : IPluginService
{
    IPluginRegistration Register(PluginUiPatchDescriptor descriptor);

    IReadOnlyList<PluginUiPatchDescriptor> ListPatches(string? pluginId = null);

    /// <summary>Computes a total order and conflict set across all active patches.</summary>
    PluginUiPatchPlan Plan();
}
