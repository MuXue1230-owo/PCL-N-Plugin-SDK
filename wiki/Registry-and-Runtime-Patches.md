# 注册表与运行时注入

> 适用于 PCL N Plugin SDK `0.2.2` 与 PCL.Plugin `v0.14.0`。

PCL N 的扩展注册表是可组合注册系统，不是 Windows 系统注册表。它用于让插件发布结构化扩展点、观察变化，并在获得目标 ACL 授权后临时覆写其他插件的注册值。所有改变宿主状态的操作都会返回注册句柄；插件停止时宿主按生命周期自动撤销。

## 权限与命名空间

- `registry.read`：读取公开或 ACL 允许的节点；
- `registry.write-own`：在 `plugins.<自己的插件 ID>` 下注册节点；
- `registry.cross-plugin`：尝试写入其他插件命名空间，仍必须通过目标节点 ACL；
- `registry.manage-acl`：管理自己节点的 ACL；
- `runtime.patch.host`：修改 PCL N 主程序方法；
- `runtime.patch.plugins`：修改其他第三方插件方法；
- `runtime.patch.transpiler`：使用 IL Transpiler。

`pcl.plugin`、`pcl.security`、签名验证和权限判定属于永久保护范围。上述任何权限都不能通过官方 API 修改、删除、覆盖或拦截这些目标。

## 注册节点

```csharp
using System.Text.Json;
using PCL.N.Plugin;

IPluginRegistryService registry = context.Services.Require<IPluginRegistryService>();
using JsonDocument initial = JsonDocument.Parse("""{"enabled":true,"port":25565}""");

IPluginRegistryRegistration registration = registry.Register(
    new PluginRegistryNodeDescriptor(
        "plugins.dev.example.terracotta.sessions.local",
        initial.RootElement.Clone(),
        [new PluginRegistryAccessRule("dev.example.overlay", PluginRegistryRights.Read)]));

context.Lifetime.Track(registration);
```

动态变化使用 `Update`，不要删除后重新注册：

```csharp
using JsonDocument next = JsonDocument.Parse("""{"enabled":true,"port":25566}""");
registration.Update(next.RootElement.Clone());
```

目标所有者可以授予其他插件 `OverrideValue`。调用 `Override` 只增加一个可撤销的合成层，不会夺走节点所有权：

```csharp
using JsonDocument value = JsonDocument.Parse("""{"enabled":false}""");
IPluginRegistration layer = registry.Override(
    "plugins.dev.example.target.feature",
    value.RootElement.Clone(),
    priority: 200);
context.Lifetime.Track(layer);
```

## Mixin 风格补丁

运行时补丁方法使用 Harmony 的 Prefix、Postfix、Finalizer 和 Transpiler 约定。插件只通过 `IPluginRuntimePatchService` 注册，宿主负责冲突顺序、归责和卸载回滚。

```csharp
using System.Reflection;
using PCL.N.Plugin;

IPluginRuntimePatchService patches = context.Services.Require<IPluginRuntimePatchService>();
MethodInfo postfix = typeof(MyMixins).GetMethod(
    nameof(MyMixins.AfterResolve),
    BindingFlags.Static | BindingFlags.NonPublic)!;

IPluginRegistration patch = patches.Register(new PluginRuntimePatchDescriptor
{
    PatchId = "observe-version-resolution",
    Target = new PluginRuntimePatchTarget(
        "PCL.Application",
        "PCL.Application.Downloads.VersionResolver",
        "ResolveAsync"),
    Postfix = postfix,
    Priority = 100
});
context.Lifetime.Track(patch);

internal static class MyMixins
{
    private static void AfterResolve(object? __result)
    {
        // 只记录必要且脱敏的信息。
    }
}
```

补丁必须使用静态方法，不得持有宿主内部对象到插件生命周期之外。目标签名必须尽量精确；重载方法应提供 `ParameterTypeNames`。注册失败时插件必须降级，而不是扫描或猜测其他私有方法。

## 安全边界

插件仍在 PCL N 进程内运行，权限不是操作系统沙箱。运行时补丁属于最高审核等级，必须经过网站签名版本审核、逐项用户授权和崩溃归责。直接捆绑补丁引擎、绕过服务或访问保护目标的包会被市场拒绝。
