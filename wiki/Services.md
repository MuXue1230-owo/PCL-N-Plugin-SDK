# 服务

> SDK `0.1.0-alpha.5`。服务通过稳定 ID 和独立版本范围协商。

插件从 `context.Services` 获取宿主服务。真正不可缺少的服务写入 Manifest 的 `services.required`；增强体验但可降级的服务写入 `services.optional`。

## 当前服务

### 核心宿主服务

| ID | C# 接口 | 用途 |
|---|---|---|
| `pcl.logging` | `IPluginLogger` | 结构化插件日志；也可直接用 `context.Logger` |
| `pcl.dispatcher` | `IPluginDispatcher` | 在宿主 UI/主线程执行操作；也可用 `context.Dispatcher` |
| `pcl.notifications` | `IPluginNotificationService` | 信息与警告提示 |
| `pcl.settings` | `IPluginSettingsStore` | 按插件隔离的键值设置 |
| `pcl.commands` | `IPluginCommandService` | 注册和调用命令 |
| `pcl.tasks` | `IPluginTaskService` | 由生命周期管理的后台任务 |
| `pcl.instances.read` | `IPluginInstanceReadService` | 只读 Minecraft实例列表 |
| `pcl.localization` | `IPluginLocalizationService` | 读取 Host当前语言和插件本地化字符串 |

这些 ID定义在 `PluginServiceIds`。`IPluginLogger` 和 `IPluginDispatcher` 同时通过 `context.Logger`、`context.Dispatcher` 提供便捷入口。

### UI与导航服务

| ID | C# 接口 | NuGet包 | 用途 |
|---|---|---|---|
| `pcl.ui` | `IPluginUiSurfaceRegistry` | `PCLN.Plugin.Abstractions` | 查询宿主发布的 UI Surface与 Slot |
| `pcl.ui.patch` | `IPluginUiPatchService` | `PCLN.Plugin.Abstractions` | 注册 Patch、规划顺序并查看冲突 |
| `pcl.navigation` | `IPluginNavigationService` | `PCLN.Plugin.UI` | 导航到已注册或 Host公开的稳定路由 |
| `pcl.ui.avalonia` | `IAvaloniaUiAccessService` | `PCLN.Plugin.UI.Avalonia` | 受权限控制地访问真实 Avalonia对象和公开 Target |
| `pcl.ui.avalonia.pages` | `IAvaloniaPluginPageService` | `PCLN.Plugin.UI.Avalonia` | 注册插件拥有的完整主导航页面并执行导航 |
| `pcl.ui.avalonia.windows` | `IAvaloniaPluginWindowService` | `PCLN.Plugin.UI.Avalonia` | 注册、显示和枚举插件拥有的窗口 |

这四个适配服务 ID定义在 `PluginUiServiceIds`。`IAvaloniaPluginPageService` 继承 `IPluginNavigationService`，因此页面服务同时提供 `Register` 和 `NavigateAsync`。

### 插件协作与宿主专用服务

| ID | C# 接口 | 用途 |
|---|---|---|
| `pcl.exports` | `IPluginExportRegistry` | 在稳定共享契约上导出或导入插件间服务 |
| `pcl.market` | Host市场契约 | 宿主管理的插件市场预留 ID |

`pcl.exports` 定义在 `PluginServiceIds`。导出契约程序集必须由默认加载上下文共享；运行时会拒绝插件私有类型越过边界。

`pcl.market` 是宿主市场契约的预留 ID；第三方插件不应假设当前 Host会通过 `context.Services` 暴露远端市场客户端。

## 公开契约与 Host可用性

NuGet包中存在接口或服务 ID，只表示插件可以针对稳定契约编译，不表示每个 PCL N或第三方 Host版本都提供实现。尤其是导航、完整 Avalonia页面、窗口、Raw Avalonia、本地化和插件导出服务，应先协商能力：

```csharp
PluginApiVersionRange uiRange = PluginApiVersionRange.Parse(">=0.1 <1.0");

if (context.Services.Supports(PluginUiServiceIds.AvaloniaPages, uiRange) &&
    context.Services.TryGet<IAvaloniaPluginPageService>(out var pages))
{
    // 注册完整页面。
}
else
{
    context.Logger.Warn("当前 Host 不提供完整 Avalonia 页面；核心功能继续运行。");
}
```

默认把可降级能力写入 `services.optional`。只有插件没有该服务就无法提供任何有效功能，并且 `host.minimumVersion` 已锁定到明确提供该服务的 Host版本时，才放入 `services.required`。

## 必需服务

Manifest：

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0",
    "pcl.settings": ">=0.1 <1.0"
  }
}
```

代码：

```csharp
IPluginCommandService commands = context.Services.Require<IPluginCommandService>();
IPluginSettingsStore settings = context.Services.Require<IPluginSettingsStore>();
```

Host 会在调用 `InitializeAsync` 之前验证 required 服务。`Require<T>()` 仍能让代码在测试或错误宿主实现中快速失败。

## 可选服务

```json
"services": {
  "optional": {
    "pcl.notifications": ">=0.1 <1.0"
  }
}
```

```csharp
if (context.Services.TryGet<IPluginNotificationService>(out var notifications))
    notifications.ShowInformation("操作完成。");
else
    context.Logger.Info("操作完成。");
```

## 设置存储

```csharp
private static readonly PluginSettingKey<string> ModeKey = new("mode");

string mode = await settings.GetAsync(ModeKey, "safe", cancellationToken);
await settings.SetAsync(ModeKey, "fast", cancellationToken);
```

Key 是当前插件命名空间内的简单名称，不得包含目录分隔符或 `..`。设置存储适合 JSON 可序列化的小型配置；大文件使用 `context.Directories.Data`。

## 命令

```csharp
IPluginRegistration registration = commands.Register(new PluginCommandDescriptor(
    "dev.example.tools.refresh",
    "刷新插件数据",
    async token => await RefreshAsync(token),
    description: "重新读取插件缓存。",
    icon: "lucide/refresh-cw"));

context.Lifetime.Track(registration);
```

命令 ID 必须全局稳定，推荐以插件 ID 开头。AXAML 按钮也通过命令 ID 绑定行为。

## 后台任务

```csharp
IPluginTaskService tasks = context.Services.Require<IPluginTaskService>();

context.Lifetime.Track(tasks.Run(
    "dev.example.tools.initial-scan",
    token => ScanAsync(token)));

context.Lifetime.Track(tasks.SchedulePeriodic(
    "dev.example.tools.refresh",
    TimeSpan.FromMinutes(10),
    token => RefreshAsync(token)));
```

宿主停用插件时会取消任务 Token 并等待结束。任务必须响应取消，不能吞掉 `OperationCanceledException` 后无限运行。

## 实例只读服务

```csharp
IPluginInstanceReadService instances = context.Services.Require<IPluginInstanceReadService>();

foreach (PluginInstanceInfo instance in instances.ListInstances())
{
    context.Logger.Info($"{instance.Id} · {instance.Name}");
}
```

返回的是公开 DTO，不是 PCL N 内部对象。不要根据 DTO 反射或猜测私有宿主类型。

## 导航、完整页面与窗口

稳定导航接口位于 `PCLN.Plugin.UI`，完整 Avalonia页面和窗口接口位于 `PCLN.Plugin.UI.Avalonia`：

```csharp
if (context.Services.TryGet<IPluginNavigationService>(out var navigation))
    await navigation.NavigateAsync("plugin/dev.example.toolbox", cancellationToken);
```

注册完整页面时使用 `IAvaloniaPluginPageService.Register(AvaloniaPluginPageDescriptor)`；注册独立窗口时使用 `IAvaloniaPluginWindowService.Register(AvaloniaPluginWindowDescriptor)`。两个 `Register` 返回值都必须交给 `context.Lifetime.Track`。完整示例和权限声明见 [UI扩展实战](Tutorial-UI-Extension)。

Raw Avalonia使用 `IAvaloniaUiAccessService`，需要明确的 `ui.raw-access` 权限。它只应用于公开 `UiTargetId`，不能作为反射或遍历宿主私有 Visual Tree的替代通道。

## 本地化

```csharp
if (context.Services.TryGet<IPluginLocalizationService>(out var localization))
{
    string title = localization.GetString("toolbox.title", "Toolbox");
    context.Logger.Debug($"{localization.CurrentCulture}: {title}");
}
```

`GetString` 的 fallback必须可直接展示，避免 Host缺少语言资源时出现空白 UI。`GetStrings` 返回的是当前 Host选择文化下的插件字符串快照，不要据此猜测宿主私有资源键。

## 插件间导出

导出方和导入方必须引用同一个稳定共享契约程序集，不能把插件私有实现类型作为公共 ABI：

```csharp
var descriptor = new PluginExportDescriptor(
    "report-provider",
    new PluginApiVersion(0, 1));

context.Lifetime.Track(exports.Export<IReportProvider>(descriptor, provider));
```

导入方：

```csharp
PluginImport<IReportProvider> import = exports.Import<IReportProvider>(
    new PluginExportId("dev.example.reports", "report-provider"),
    PluginApiVersionRange.Parse(">=0.1 <1.0"));

if (import.IsAvailable)
    await import.Require().GenerateAsync(cancellationToken);
```

`pcl.exports` 应作为可选服务，并且插件依赖关系仍须在 Manifest中声明；Export Registry不能替代依赖解析和版本约束。

## Dispatcher

```csharp
await context.Dispatcher.InvokeAsync(
    () => notifications.ShowInformation("已切回 UI 线程。"),
    cancellationToken);
```

只在需要操作宿主 UI 的地方切换线程；文件、网络和 CPU 工作不要占用 UI 线程。

## 查询服务版本

```csharp
PluginApiVersionRange range = PluginApiVersionRange.Parse(">=0.1 <1.0");
bool available = context.Services.Supports(PluginServiceIds.Ui, range);
```

服务范围使用空格分隔约束，例如 `>=0.1 <1.0`。不要把 NuGet 包版本 `0.1.0-alpha.5` 写成服务版本；两者独立演进。
