# 实战案例集

> Applies to PCL N Plugin SDK 0.2.0.

> 以下片段基于 SDK `0.2.0`。示例中的服务应同步声明到 `plugin.json`。

## 案例 1：注册一个命令并显示通知

```csharp
using PCL.N.Plugin;

IPluginCommandService commands = context.Services.Require<IPluginCommandService>();
IPluginNotificationService notifications = context.Services.Require<IPluginNotificationService>();

context.Lifetime.Track(commands.Register(new PluginCommandDescriptor(
    id: "dev.example.tools.say-hello",
    title: "打个招呼",
    executeAsync: cancellationToken =>
    {
        cancellationToken.ThrowIfCancellationRequested();
        notifications.ShowInformation("你好，PCL N！");
        return Task.CompletedTask;
    },
    description: "显示一条测试通知。",
    icon: "lucide/message-circle")));
```

Manifest：

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0",
    "pcl.notifications": ">=0.1 <1.0"
  }
},
"permissions": [
  { "id": "pcl.commands", "reason": "注册插件命令。" },
  { "id": "pcl.notifications", "reason": "显示命令执行结果。" }
]
```

## 案例 2：保存和读取插件设置

```csharp
private static readonly PluginSettingKey<bool> EnabledKey = new("enabled");

IPluginSettingsStore settings = context.Services.Require<IPluginSettingsStore>();
bool enabled = await settings.GetAsync(EnabledKey, defaultValue: true, cancellationToken);

if (!enabled)
    context.Logger.Info("该功能已由用户关闭。");

await settings.SetAsync(EnabledKey, false, cancellationToken);
```

设置存储按插件隔离。Key 只能是简单名称，不能含 `/`、`\\` 或 `..`。

## 案例 3：运行可追踪的后台任务

不要使用裸 `Task.Run`、`Thread`、`Timer` 或 `FileSystemWatcher`。使用宿主任务服务：

```csharp
IPluginTaskService tasks = context.Services.Require<IPluginTaskService>();

IPluginTaskRegistration task = tasks.SchedulePeriodic(
    "dev.example.tools.refresh",
    TimeSpan.FromMinutes(5),
    async stoppingToken =>
    {
        await RefreshCacheAsync(stoppingToken);
        context.Logger.Debug("缓存刷新完成。");
    });

context.Lifetime.Track(task);
```

插件停用时宿主会取消 Token、等待任务结束并释放注册项。

## 案例 4：读取 Minecraft 实例列表

```csharp
if (context.Services.TryGet<IPluginInstanceReadService>(out var instances))
{
    foreach (PluginInstanceInfo instance in instances.ListInstances())
        context.Logger.Info($"{instance.Name}: {instance.InstanceDirectory}");
}
else
{
    context.Logger.Warn("当前宿主没有提供实例读取服务。");
}
```

Manifest 把它声明为可选服务，并声明读取权限：

```json
"services": {
  "optional": { "pcl.instances.read": ">=0.1 <1.0" }
},
"permissions": [
  { "id": "pcl.instances.read", "reason": "列出实例名称以生成报告。" }
]
```

## 案例 5：注册一个设置页

```csharp
using PCL.N.Plugin.Sdk;

if (context.Capabilities.TryGet<IPluginSettingsPageCapability>(out var pages))
{
    context.Lifetime.Track(pages.Register(new PluginSettingsPageDescriptor(
        "dev.example.tools.settings",
        "示例工具",
        "lucide/wrench",
        "示例工具设置",
        "管理示例工具的行为。",
        [
            new PluginSettingsHintDescriptor(
                "这是由第三方插件注册的页面。",
                PluginSettingsHintKind.Information)
        ])
    {
        Order = 500
    }));
}
```

Capability 可能因宿主模式而不存在，适合用 `TryGet` 做降级。注册结果仍必须交给 Lifetime。

## 案例 6：向启动页注入声明式 AXAML

`plugin.json`：

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0",
    "pcl.ui": ">=0.1 <1.0"
  }
},
"permissions": [
  { "id": "pcl.commands", "reason": "处理面板按钮。" },
  { "id": "ui.inject", "reason": "在启动页显示工具面板。" }
],
"ui": {
  "api": { "minimum": "0.1", "maximumExclusive": "1.0" },
  "avalonia": { "minimum": "12.0", "maximumExclusive": "13.0" },
  "targets": [
    {
      "target": "pcl.page.launch",
      "surface": ">=3.0 <4.0",
      "access": ["inject"],
      "operations": [
        {
          "id": "tools-panel",
          "kind": "inject",
          "slot": "primary-actions.after",
          "axaml": "ui/ToolsPanel.axaml",
          "command": "dev.example.tools.open",
          "required": false,
          "fallback": "disable-feature"
        }
      ]
    }
  ]
}
```

`ui/ToolsPanel.axaml`：

```xml
<Border xmlns="https://github.com/avaloniaui"
        Padding="12"
        CornerRadius="8">
  <StackPanel Spacing="8">
    <TextBlock FontWeight="SemiBold" Text="示例工具" />
    <Button Content="打开"
            Command="{Binding Commands[dev.example.tools.open]}" />
  </StackPanel>
</Border>
```

AXAML 不得包含 `x:Class` 或代码隐藏。行为由公开命令 ID 连接。

## 案例 7：在不同宿主版本中优雅降级

```csharp
PluginApiVersionRange range = PluginApiVersionRange.Parse(">=0.1 <1.0");

if (context.Services.Supports(PluginServiceIds.Notifications, range) &&
    context.Services.TryGet<IPluginNotificationService>(out var notifications))
{
    notifications.ShowInformation("通知服务可用。");
}
else
{
    context.Logger.Info("通知服务不可用，改为只写日志。");
}
```

真正不可缺少的能力放进 `services.required`；增强体验的能力放进 `services.optional`。

## 案例 8：声明前置与不兼容插件

```json
"dependencies": [
  {
    "id": "dev.example.core",
    "version": ">=1.2.0 <2.0.0",
    "kind": "required"
  },
  {
    "id": "dev.example.theme",
    "version": ">=0.5.0",
    "kind": "optional"
  }
],
"incompatibilities": [
  {
    "id": "dev.example.legacy",
    "version": "<2.0.0",
    "reason": "两者会修改同一个启动页区域。"
  }
]
```

运行时会先加载 required 前置，并在检测到已启用的不兼容版本时默认禁用冲突插件。

## 案例 9：用内存宿主测试命令

```csharp
using PCL.N.Plugin;
using PCL.N.Plugin.Testing;

[TestMethod]
public async Task Initialize_registers_and_executes_command()
{
    PluginDescriptor descriptor = new(
        new PluginId("dev.example.tools"),
        "Example Tools",
        PluginVersion.Parse("0.1.0"));

    await using TestPluginContext context = new(descriptor, new PluginApiVersion(0, 2));
    TestPluginCommandService commands = new();
    TestPluginNotificationService notifications = new();
    context.TestServices.Add<IPluginCommandService>(commands);
    context.TestServices.Add<IPluginNotificationService>(notifications);

    ExamplePlugin plugin = new();
    await plugin.InitializeAsync(context, CancellationToken.None);
    await commands.InvokeAsync("dev.example.tools.say-hello");

    CollectionAssert.Contains(notifications.Messages.ToArray(), "info:你好，PCL N！");
}
```

更多测试模式见 [测试插件](Testing-Plugins)。

## 案例 10：读取游戏会话并订阅输出

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
```

Manifest 通常把它们声明为 optional，并给 `pcl.game.output` 明确权限原因。

## 案例 11：受控进程、隔离文件和剪贴板

```csharp
IPluginProcessService processes = context.Services.Require<IPluginProcessService>();
IPluginFileService files = context.Services.Require<IPluginFileService>();

PluginProcessResult result = await processes.RunAsync(new PluginProcessRequest
{
    FileName = "java",
    Arguments = ["-version"],
    CaptureOutput = true,
    Timeout = TimeSpan.FromSeconds(10)
}, cancellationToken);

await files.WriteAsync("reports/java-version.txt", Encoding.UTF8.GetBytes(result.StandardError), cancellationToken);

if (context.Services.TryGet<IPluginClipboardService>(out var clipboard))
    await clipboard.WriteTextAsync(result.StandardError, cancellationToken);
```

不要直接调用 `Process.Start` 或自行拼接宿主目录；通过 Host 服务才能获得权限提示、审计和路径隔离。

## 案例 12：注册启动参数修改器

```csharp
IPluginLaunchModificationService launchModify =
    context.Services.Require<IPluginLaunchModificationService>();

context.Lifetime.Track(launchModify.Register(new PluginLaunchModification(
    "dev.example.tools.add-launch-flag",
    request => request with
    {
        GameArguments = request.GameArguments.Concat(["--example-flag"]).ToArray()
    })));
```

启动修改器应是可重复的纯函数，不要在 `Apply` 委托里写文件、弹窗或启动后台任务。

## 案例 13：携带多平台原生库

```xml
<ItemGroup>
  <PclNPluginNative Include="native/win-x64/example.dll"
                    RuntimeIdentifier="win-x64" />
  <PclNPluginNative Include="native/linux-x64/libexample.so"
                    RuntimeIdentifier="linux-x64" />
  <PclNPluginNative Include="native/osx-arm64/libexample.dylib"
                    RuntimeIdentifier="osx-arm64" />
</ItemGroup>
```

构建器会放入 `runtimes/<rid>/native/`。同时在 Manifest 的 `platforms` 中声明实际支持的系统、架构和 RID。纯托管插件应保持 `AnyCPU`。