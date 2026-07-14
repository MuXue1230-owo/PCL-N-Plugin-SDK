# 服务

> SDK `0.1.0-alpha.5`。服务通过稳定 ID 和独立版本范围协商。

插件从 `context.Services` 获取宿主服务。真正不可缺少的服务写入 Manifest 的 `services.required`；增强体验但可降级的服务写入 `services.optional`。

## 当前服务

| ID | C# 接口 | 用途 |
|---|---|---|
| `pcl.logging` | `IPluginLogger` | 结构化插件日志；也可直接用 `context.Logger` |
| `pcl.dispatcher` | `IPluginDispatcher` | 在宿主 UI/主线程执行操作；也可用 `context.Dispatcher` |
| `pcl.notifications` | `IPluginNotificationService` | 信息与警告提示 |
| `pcl.settings` | `IPluginSettingsStore` | 按插件隔离的键值设置 |
| `pcl.commands` | `IPluginCommandService` | 注册和调用命令 |
| `pcl.tasks` | `IPluginTaskService` | 由生命周期管理的后台任务 |
| `pcl.instances.read` | `IPluginInstanceReadService` | 只读 Minecraft 实例列表 |
| `pcl.ui` | `IPluginUiSurfaceRegistry` | 查询宿主发布的 UI Surface 与 Slot |
| `pcl.ui.patch` | `IPluginUiPatchService` | 注册 Patch、规划顺序并查看冲突 |

`pcl.market` 是宿主市场契约的预留 ID；第三方插件不应假设当前 Host 会通过 `context.Services` 暴露远端市场客户端。

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
