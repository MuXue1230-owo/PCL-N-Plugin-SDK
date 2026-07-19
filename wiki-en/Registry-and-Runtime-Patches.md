# Registry and Runtime Patches

> Applies to PCL N Plugin SDK `0.2.3` and PCL.Plugin `v0.14.0`.

The PCL registry is a composable extension registry, not the Windows system Registry. Plugins publish immutable JSON nodes, observe changes, and may add reversible overrides to another plugin only when both the effective permission and the target ACL allow it. Every mutation returns a lifetime-owned registration.

## Permissions and namespaces

- `registry.read`: read visible nodes;
- `registry.write-own`: register below `plugins.<own-plugin-id>`;
- `registry.cross-plugin`: request a cross-plugin write, still subject to the target ACL;
- `registry.manage-acl`: manage ACLs on owned nodes;
- `runtime.patch.host`: patch PCL N host methods;
- `runtime.patch.plugins`: patch another third-party plugin;
- `runtime.patch.transpiler`: register an IL transpiler.

`pcl.plugin`, `pcl.security`, signature verification, and permission enforcement are permanently protected targets. No public permission grants write, remove, override, or patch access to them.

## Registering a node

```csharp
using System.Text.Json;
using PCL.N.Plugin;

IPluginRegistryService registry = context.Services.Require<IPluginRegistryService>();
using JsonDocument json = JsonDocument.Parse("""{"enabled":true,"port":25565}""");
IPluginRegistryRegistration node = registry.Register(
    new PluginRegistryNodeDescriptor(
        "plugins.dev.example.terracotta.sessions.local",
        json.RootElement.Clone(),
        [new PluginRegistryAccessRule("dev.example.overlay", PluginRegistryRights.Read)]));
context.Lifetime.Track(node);
```

Use `node.Update(nextValue)` for live state. `Override(path, value, priority)` adds a reversible composition layer and never transfers ownership of the target node.

## Mixin-style patch

Patch methods follow Harmony Prefix, Postfix, Finalizer, and Transpiler conventions. The host owns ordering, attribution, and unpatching.

```csharp
IPluginRuntimePatchService patches = context.Services.Require<IPluginRuntimePatchService>();
IPluginRegistration patch = patches.Register(new PluginRuntimePatchDescriptor
{
    PatchId = "observe-version-resolution",
    Target = new PluginRuntimePatchTarget(
        "PCL.Application",
        "PCL.Application.Downloads.VersionResolver",
        "ResolveAsync"),
    Postfix = typeof(MyMixins).GetMethod(
        "AfterResolve",
        BindingFlags.Static | BindingFlags.NonPublic)
});
context.Lifetime.Track(patch);
```

Runtime patch permissions are the highest review tier. Packages must use the host service; bundling an independent patch engine or targeting protected platform code is rejected by market review. These controls reduce risk but do not turn in-process .NET plugins into an operating-system sandbox.

Provide `ParameterTypeNames` for overloaded methods and `TargetPluginId` when patching another plugin, so identically named assemblies in separate load contexts cannot make the target ambiguous.
