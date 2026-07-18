# 服务

> SDK `0.2.1`。服务通过稳定 ID 和独立版本范围协商。

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
| `pcl.secure-storage` | `IPluginSecureStorage` | Host托管的插件隔离凭据存储 |
| `pcl.uri-launcher` | `IPluginUriLauncher` | 通过 Host 打开外部 HTTP/HTTPS 链接 |
| `pcl.background-tasks` | `IPluginBackgroundTaskService` | 启动器任务管理进度（与 MC 安装下载同界面） |

这些 ID定义在 `PluginServiceIds`。`IPluginLogger` 和 `IPluginDispatcher` 同时通过 `context.Logger`、`context.Dispatcher` 提供便捷入口。

### 任务管理进度（`pcl.background-tasks`）

用于插件自己的下载、安装、更新等长任务，进度会进入宿主「任务管理」页（与 Minecraft 安装相同 UI）：

```csharp
IPluginBackgroundTaskService tasks = context.Services.Require<IPluginBackgroundTaskService>();
using IPluginBackgroundTask task = tasks.Begin("下载资源包", openTaskManager: true);
try
{
    task.Report(new PluginBackgroundTaskProgress("下载中", "1.2 MB / 4.0 MB", Progress: 0.3, SpeedBytesPerSecond: 120_000));
    // ... 使用 task.Token 支持用户取消
    task.Complete("下载完成");
}
catch (OperationCanceledException)
{
    task.Fail("已取消", canceled: true);
}
```

建议在 Manifest `services.optional` 中声明 `"pcl.background-tasks": ">=0.1 <1.0"`，以便在不支持该服务的 Host 上降级为静默进度或日志。

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

## 安全存储与外部链接

安全存储适合保存访问令牌、刷新令牌等不能落入明文设置文件的插件私密数据。Key 位于当前插件命名空间内，Host 必须使用系统级安全设施或返回 `Unavailable`，不能静默降级为明文持久化。

```csharp
IPluginSecureStorage secureStorage = context.Services.Require<IPluginSecureStorage>();
PluginSecretKey tokenKey = new("auth.token");

await secureStorage.WriteAsync(tokenKey, tokenBytes, cancellationToken);
PluginSecretReadResult read = await secureStorage.ReadAsync(tokenKey, cancellationToken);
if (read.Status == PluginSecureStorageStatus.Success && read.Value is not null)
{
    // 使用 read.Value 后尽快清零敏感缓冲区。
}
```

外部链接应通过 `IPluginUriLauncher` 交给 Host 打开，避免插件直接启动进程或绕过 Host 的确认、审计与协议限制。契约只接受绝对 `http` / `https` URI。

```csharp
if (context.Services.TryGet<IPluginUriLauncher>(out var launcher))
    await launcher.OpenAsync(new Uri("https://example.com/help"), cancellationToken);
```

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

## 游戏会话、输出与启动事件

这些服务是只读观察面，适合日志面板、统计、自动诊断和联机状态提示。它们不会暴露 PCL N 内部对象。

```csharp
if (context.Services.TryGet<IPluginGameSessionService>(out var sessions))
{
    foreach (PluginGameSessionSnapshot session in sessions.ListSessions())
        context.Logger.Info($"{session.InstanceId}: {session.State}");
}

if (context.Services.TryGet<IPluginGameOutputService>(out var output))
{
    context.Lifetime.Track(output.Subscribe(line =>
        context.Logger.Debug($"[{line.Stream}] {line.Text}")));
}

if (context.Services.TryGet<IPluginLaunchEventService>(out var launches))
{
    context.Lifetime.Track(launches.Subscribe(evt =>
        context.Logger.Info($"Launch event: {evt.Kind} / {evt.Session.InstanceId}")));
}
```

Manifest 中通常把它们声明为 optional；如果插件的核心功能就是会话监控，才放入 required。需要给出面向用户的权限原因，例如“读取游戏输出以生成崩溃报告”。

## 受控进程、文件与剪贴板

`IPluginProcessService` 通过 Host 策略启动子进程。插件不应直接调用 `Process.Start`；受控服务可以记录审计、限制工作目录、超时和输出大小。

```csharp
IPluginProcessService processes = context.Services.Require<IPluginProcessService>();
PluginProcessResult result = await processes.RunAsync(new PluginProcessRequest
{
    FileName = "java",
    Arguments = ["-version"],
    CaptureOutput = true,
    Timeout = TimeSpan.FromSeconds(10)
}, cancellationToken);

context.Logger.Info(result.StandardError);
```

`IPluginFileService` 只允许访问插件隔离数据目录内的相对路径，适合保存插件生成的报告、索引和导出文件：

```csharp
IPluginFileService files = context.Services.Require<IPluginFileService>();
await files.WriteAsync("reports/latest.txt", Encoding.UTF8.GetBytes("ok"), cancellationToken);
byte[]? content = await files.ReadAsync("reports/latest.txt", cancellationToken);
```

剪贴板读写必须通过 `IPluginClipboardService`，避免插件绕过 Host 的用户确认或隐私策略：

```csharp
if (context.Services.TryGet<IPluginClipboardService>(out var clipboard))
    await clipboard.WriteTextAsync("诊断报告已生成", cancellationToken);
```

## 账户、下载源与启动修改

`IPluginAccountReadService` 和 `IPluginDownloadService` 是只读目录服务，只返回公开 DTO：

```csharp
if (context.Services.TryGet<IPluginAccountReadService>(out var accounts))
{
    foreach (PluginAccountProviderInfo provider in accounts.ListProviders())
        context.Logger.Info($"Account provider: {provider.DisplayName}");
}

if (context.Services.TryGet<IPluginDownloadService>(out var downloads))
{
    foreach (PluginDownloadSourceInfo source in downloads.ListSources())
        context.Logger.Info($"Download source: {source.DisplayName} ({source.Kind})");
}
```

启动修改通过 `IPluginLaunchModificationService` 注册纯函数，返回新的 `PluginLaunchRequest`。修改器必须可重复、无副作用，并把注册项交给 Lifetime：

```csharp
IPluginLaunchModificationService launchModify =
    context.Services.Require<IPluginLaunchModificationService>();

context.Lifetime.Track(launchModify.Register(new PluginLaunchModification(
    "dev.example.add-demo-flag",
    request => request with
    {
        GameArguments = request.GameArguments.Concat(["--demo"]).ToArray()
    })));
```

## 导航、完整页面与窗口

稳定导航接口位于 `PCLN.Plugin.UI`，完整 Avalonia页面和窗口接口位于 `PCLN.Plugin.UI.Avalonia`：

```csharp
if (context.Services.TryGet<IPluginNavigationService>(out var navigation))
    await navigation.NavigateAsync("dev.example.toolbox.page", cancellationToken);
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

服务范围使用空格分隔约束，例如 `>=0.1 <1.0`。不要把 NuGet 包版本 `0.1.0` 写成服务版本；两者独立演进。
