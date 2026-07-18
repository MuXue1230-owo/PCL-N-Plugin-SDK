# 服务、设置与后台任务实战

> 适用于 SDK `0.2.1`。本教程把前一章的 Toolbox 扩展为“实例检查器”。

目标功能：保存扫描间隔、注册手动扫描命令、定期读取 Minecraft 实例，并在宿主提供通知服务时显示结果。

## 1. 先决定 required 与 optional

不要看到服务就全部写成 required。判断标准是“缺少它时插件核心功能还能不能工作”。

| 服务 | 本例策略 | 原因 |
|---|---|---|
| `pcl.commands` | required | 用户需要手动触发扫描 |
| `pcl.settings` | required | 必须保存扫描间隔 |
| `pcl.tasks` | required | 自动扫描由宿主管理生命周期 |
| `pcl.instances.read` | required | 核心功能就是读取实例 |
| `pcl.notifications` | optional | 缺失时可以改写日志 |

Manifest 片段：

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0",
    "pcl.settings": ">=0.1 <1.0",
    "pcl.tasks": ">=0.1 <1.0",
    "pcl.instances.read": ">=0.1 <1.0"
  },
  "optional": {
    "pcl.notifications": ">=0.1 <1.0"
  }
},
"permissions": [
  {
    "id": "pcl.instances.read",
    "reason": "统计本地 Minecraft 实例并在日志中列出名称。"
  },
  {
    "id": "pcl.notifications",
    "reason": "在扫描完成后显示实例数量。"
  }
]
```

服务声明负责兼容协商，权限声明负责向用户解释敏感访问。即使两者 ID 相同，也不能省略其中一边。

## 2. 定义类型安全的设置键

```csharp
private static readonly PluginSettingKey<int> ScanMinutesKey =
    new("scan-minutes");
```

设置 Key 只在当前插件的数据空间内生效。名称应稳定、简短，不得包含 `/`、`\` 或 `..`。如果未来重命名 Key，需要自行迁移旧值。

## 3. 完整入口实现

```csharp
using PCL.N.Plugin;

namespace PclNToolbox.Plugin;

public sealed class ToolboxPlugin : IPclNPlugin
{
    private static readonly PluginSettingKey<int> ScanMinutesKey =
        new("scan-minutes");

    public async ValueTask InitializeAsync(
        IPluginContext context,
        CancellationToken cancellationToken)
    {
        IPluginCommandService commands =
            context.Services.Require<IPluginCommandService>();
        IPluginSettingsStore settings =
            context.Services.Require<IPluginSettingsStore>();
        IPluginTaskService tasks =
            context.Services.Require<IPluginTaskService>();
        IPluginInstanceReadService instances =
            context.Services.Require<IPluginInstanceReadService>();

        int scanMinutes = await settings.GetAsync(
            ScanMinutesKey,
            defaultValue: 15,
            cancellationToken);
        scanMinutes = Math.Clamp(scanMinutes, 1, 24 * 60);

        async Task ScanAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            IReadOnlyList<PluginInstanceInfo> result = instances.ListInstances();

            foreach (PluginInstanceInfo instance in result)
            {
                token.ThrowIfCancellationRequested();
                context.Logger.Info($"实例：{instance.Name} ({instance.Id})");
            }

            if (context.Services.TryGet<IPluginNotificationService>(
                    out var notifications))
            {
                await context.Dispatcher.InvokeAsync(
                    () => notifications.ShowInformation(
                        $"实例扫描完成，共 {result.Count} 个。"),
                    token);
            }
            else
            {
                context.Logger.Info($"实例扫描完成，共 {result.Count} 个。");
            }
        }

        context.Lifetime.Track(commands.Register(
            new PluginCommandDescriptor(
                "dev.example.toolbox.scan-instances",
                "Toolbox：扫描实例",
                ScanAsync,
                description: "列出当前 PCL N 管理的 Minecraft 实例。",
                icon: "lucide/search")));

        context.Lifetime.Track(tasks.SchedulePeriodic(
            "dev.example.toolbox.periodic-instance-scan",
            TimeSpan.FromMinutes(scanMinutes),
            ScanAsync));

        context.Logger.Info($"自动扫描间隔：{scanMinutes} 分钟。");
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
```

## 4. 为什么这样组织

### 注册项统一交给 Lifetime

命令和定时任务都会跨过 `InitializeAsync` 的调用范围继续存在。把注册项交给 `context.Lifetime.Track` 后，宿主会在停用、卸载或初始化回滚时按逆序释放它们。

不要只把注册项保存在静态字段中，也不要假设进程退出才会清理。

### 后台任务必须响应取消

宿主停用插件时会取消任务 Token 并等待结束。循环、网络请求和文件操作都应传递或检查这个 Token。不要捕获 `OperationCanceledException` 后继续无限运行。

### UI 调用切回 Dispatcher

任务回调可能不在 UI 线程。通知或界面更新通过 `context.Dispatcher` 执行；文件扫描、JSON 解析等工作仍留在后台线程。

### 只保存配置，不保存大文件

`IPluginSettingsStore` 适合字符串、数字、布尔值和小型 JSON 配置。缓存、索引、导出文件应放在：

```csharp
string cacheFile = Path.Combine(context.Directories.Cache, "instances.json");
string dataFile = Path.Combine(context.Directories.Data, "rules.json");
```

不要自行写入启动器安装目录，也不要访问其他插件的数据目录。

## 5. 增加修改设置的命令

例如把扫描间隔重置为 30 分钟：

```csharp
context.Lifetime.Track(commands.Register(
    new PluginCommandDescriptor(
        "dev.example.toolbox.use-30-minute-scan",
        "Toolbox：每 30 分钟扫描",
        async token =>
        {
            await settings.SetAsync(ScanMinutesKey, 30, token);
            context.Logger.Info("扫描间隔已保存；重新启用插件后生效。");
        })));
```

如果需要立即生效，应保存当前任务注册项，先释放旧任务再创建新任务；不要让多个周期任务叠加运行。

## 6. 测试降级路径

至少验证两种宿主组合：

1. required 服务齐全，并注入通知服务：命令成功且产生通知。
2. required 服务齐全，但不注入通知服务：命令仍成功，结果进入日志。

`PCLN.Plugin.Testing` 已提供内存版命令、通知、设置和实例服务。详细测试代码见 [测试、调试与发布实战](Tutorial-Test-Debug-Release)。

## 6. 完整应用实例：启动诊断助手

下面的插件组合了实例读取、游戏会话、输出订阅、受控进程、文件、剪贴板、账户/下载源枚举和启动修改。它适合做“启动诊断助手”：启动前添加诊断参数，运行中收集输出，用户可一键导出报告。

Manifest 片段：

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0",
    "pcl.files": ">=0.1 <1.0",
    "pcl.launch.modify": ">=0.1 <1.0"
  },
  "optional": {
    "pcl.instances.read": ">=0.1 <1.0",
    "pcl.game.sessions": ">=0.1 <1.0",
    "pcl.game.output": ">=0.1 <1.0",
    "pcl.launch.events": ">=0.1 <1.0",
    "pcl.process": ">=0.1 <1.0",
    "pcl.clipboard": ">=0.1 <1.0",
    "pcl.accounts.read": ">=0.1 <1.0",
    "pcl.downloads": ">=0.1 <1.0",
    "pcl.notifications": ">=0.1 <1.0"
  }
},
"permissions": [
  { "id": "pcl.files", "reason": "保存启动诊断报告。" },
  { "id": "pcl.launch.modify", "reason": "为启动参数追加诊断标记。" },
  { "id": "pcl.game.output", "reason": "收集游戏输出以定位启动失败。" },
  { "id": "pcl.process", "reason": "运行受控诊断命令。" },
  { "id": "pcl.clipboard", "reason": "把报告摘要复制给用户。" }
]
```

核心实现：

```csharp
public sealed class LaunchDoctorPlugin : IPclNPlugin
{
    public ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        IPluginCommandService commands = context.Services.Require<IPluginCommandService>();
        IPluginFileService files = context.Services.Require<IPluginFileService>();
        IPluginLaunchModificationService launchModify =
            context.Services.Require<IPluginLaunchModificationService>();

        context.Lifetime.Track(launchModify.Register(new PluginLaunchModification(
            "dev.example.launch-doctor.add-flag",
            request => request with
            {
                GameArguments = request.GameArguments.Concat(["--pcln-diagnostics"]).ToArray()
            })));

        if (context.Services.TryGet<IPluginGameOutputService>(out var output))
        {
            context.Lifetime.Track(output.Subscribe(line =>
            {
                if (line.Stream == PluginGameOutputChannel.StandardError)
                    context.Logger.Warn(line.Text);
            }));
        }

        context.Lifetime.Track(commands.Register(new PluginCommandDescriptor(
            "dev.example.launch-doctor.export",
            "导出启动诊断报告",
            async token =>
            {
                List<string> lines = [];

                if (context.Services.TryGet<IPluginInstanceReadService>(out var instances))
                {
                    foreach (PluginInstanceInfo instance in instances.ListInstances())
                        lines.Add($"Instance: {instance.Id} / {instance.Name}");
                }

                if (context.Services.TryGet<IPluginGameSessionService>(out var sessions))
                {
                    foreach (PluginGameSessionSnapshot session in sessions.ListSessions())
                        lines.Add($"Session: {session.SessionId} / {session.State} / {session.ExitCode}");
                }

                if (context.Services.TryGet<IPluginAccountReadService>(out var accounts))
                {
                    foreach (PluginAccountProviderInfo provider in accounts.ListProviders())
                        lines.Add($"Account provider: {provider.DisplayName}");
                }

                if (context.Services.TryGet<IPluginDownloadService>(out var downloads))
                {
                    foreach (PluginDownloadSourceInfo source in downloads.ListSources())
                        lines.Add($"Download source: {source.DisplayName} ({source.Kind})");
                }

                if (context.Services.TryGet<IPluginProcessService>(out var processes))
                {
                    PluginProcessResult java = await processes.RunAsync(new PluginProcessRequest
                    {
                        FileName = "java",
                        Arguments = ["-version"],
                        CaptureOutput = true,
                        Timeout = TimeSpan.FromSeconds(10)
                    }, token);
                    lines.Add("java -version: " + java.StandardError.Trim());
                }

                string report = string.Join(Environment.NewLine, lines);
                await files.WriteAsync("reports/launch-doctor.txt", Encoding.UTF8.GetBytes(report), token);

                if (context.Services.TryGet<IPluginClipboardService>(out var clipboard))
                    await clipboard.WriteTextAsync(report, token);
                if (context.Services.TryGet<IPluginNotificationService>(out var notifications))
                    notifications.ShowInformation("启动诊断报告已生成。");
            },
            description: "收集实例、会话、账户、下载源和 Java 诊断信息。")));

        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
```

这个例子体现了 0.1.0 的推荐模式：核心能力 required，观察/增强能力 optional；所有长生命周期注册都交给 `context.Lifetime.Track`；文件写入使用 `IPluginFileService`，进程启动使用 `IPluginProcessService`。

## 常见问题

| 问题 | 处理方式 |
|---|---|
| `Require<T>()` 在初始化时失败 | 检查 Manifest 是否把该服务声明为 required，以及宿主版本是否满足范围 |
| 停用插件后命令还存在 | 检查是否跟踪了 `Register` 返回值 |
| 自动任务无法停下 | 把取消 Token 传到所有可取消操作，并检查循环中的取消状态 |
| UI 偶发线程异常 | 只把 UI 更新放进 Dispatcher，耗时工作不要放进去 |
| 设置读取失败 | 确认类型没有变化；若从 `int` 改为 `string`，需要显式迁移 |

下一步可以加入 [UI 扩展](Tutorial-UI-Extension)，为扫描间隔和结果提供设置页或启动页面板。
