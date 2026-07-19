using System.Reflection;

namespace PCL.N.Plugin;

/// <summary>A stable string-based target so plugins do not reference PCL implementation assemblies.</summary>
public sealed record PluginRuntimePatchTarget(
    string AssemblyName,
    string TypeName,
    string MethodName,
    IReadOnlyList<string>? ParameterTypeNames = null,
    int? GenericArity = null,
    string? TargetPluginId = null);

/// <summary>
/// Harmony-compatible patch methods. Prefix, postfix and finalizer are ordinary static methods using
/// Harmony argument conventions. Transpilers require the separately declared high-risk permission.
/// </summary>
public sealed record PluginRuntimePatchDescriptor
{
    public required string PatchId { get; init; }

    public required PluginRuntimePatchTarget Target { get; init; }

    public MethodInfo? Prefix { get; init; }

    public MethodInfo? Postfix { get; init; }

    public MethodInfo? Transpiler { get; init; }

    public MethodInfo? Finalizer { get; init; }

    public int Priority { get; init; }

    public IReadOnlyList<string> Before { get; init; } = [];

    public IReadOnlyList<string> After { get; init; } = [];
}

public sealed record PluginRuntimePatchInfo(
    string GlobalPatchId,
    PluginRuntimePatchTarget Target,
    bool HasPrefix,
    bool HasPostfix,
    bool HasTranspiler,
    bool HasFinalizer,
    int Priority);

/// <summary>
/// Trusted Mixin-style runtime patching. Every patch is owned by the caller and automatically
/// removed with its plugin lifetime. PCL.Plugin and the permission/signature pipeline are protected targets.
/// </summary>
public interface IPluginRuntimePatchService : IPluginService
{
    IPluginRegistration Register(PluginRuntimePatchDescriptor descriptor);

    IReadOnlyList<PluginRuntimePatchInfo> ListOwned();
}
