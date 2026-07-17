# 测试插件

> Applies to PCL N Plugin SDK 0.2.0.

> SDK `0.2.0`

`PCLN.Plugin.Testing` 提供不启动 PCL N 的内存宿主。你可以验证初始化、服务调用、命令、设置、任务、UI 注册和生命周期释放。

## 创建测试项目

```powershell
dotnet new mstest -n ExamplePlugin.Tests --framework net10.0
dotnet add ExamplePlugin.Tests package PCLN.Plugin.Testing --version 0.1.0
dotnet add ExamplePlugin.Tests reference ExamplePlugin.csproj
```

测试包只应进入测试项目，不要放进 `.pnp`。

## 创建测试 Context

```csharp
using PCL.N.Plugin;
using PCL.N.Plugin.Testing;

PluginDescriptor descriptor = new(
    new PluginId("dev.example.plugin"),
    "Example Plugin",
    PluginVersion.Parse("0.1.0"));

await using TestPluginContext context = new(
    descriptor,
    new PluginApiVersion(0, 2));
```

Context 自动提供：

- 独立临时目录；
- 收集日志的 `CollectingPluginLogger`；
- 立即执行的 Dispatcher；
- 可注入的服务和 Capability 容器；
- 逆序释放注册项的测试 Lifetime。

## 测试命令与通知

```csharp
[TestMethod]
public async Task Plugin_registers_command_and_shows_notification()
{
    PluginDescriptor descriptor = new(
        new PluginId("dev.example.plugin"),
        "Example Plugin",
        PluginVersion.Parse("0.1.0"));

    await using TestPluginContext context = new(descriptor, new PluginApiVersion(0, 2));
    TestPluginCommandService commands = new();
    TestPluginNotificationService notifications = new();
    context.TestServices.Add<IPluginCommandService>(commands);
    context.TestServices.Add<IPluginNotificationService>(notifications);

    ExamplePlugin plugin = new();
    await plugin.InitializeAsync(context, CancellationToken.None);

    Assert.AreEqual(1, commands.Commands.Count);
    await commands.InvokeAsync("dev.example.plugin.run");
    CollectionAssert.Contains(notifications.Messages.ToArray(), "info:完成");
}
```

## 测试设置

```csharp
TestPluginSettingsStore settings = new();
context.TestServices.Add<IPluginSettingsStore>(settings);

PluginSettingKey<int> key = new("refresh-minutes");
await settings.SetAsync(key, 15);
Assert.AreEqual(15, await settings.GetAsync(key, 5));
```

## 测试实例读取

```csharp
TestPluginInstanceReadService instances = new(
[
    new PluginInstanceInfo("survival", "生存服", @"D:\Games\Minecraft\survival")
]);

context.TestServices.Add<IPluginInstanceReadService>(instances);
```

测试不要依赖该示例 Windows 路径真实存在；它只是 DTO 数据。

## 测试扩展服务

`TestPluginContext` 默认提供 0.1.0 的 Stable 服务替身，可直接验证进程、文件、剪贴板、账户、下载源和启动修改逻辑：

```csharp
await using TestPluginContext context = new(descriptor, new PluginApiVersion(0, 2));

context.Process.EnqueueResult(new PluginProcessResult(0, "ok", ""));
PluginProcessResult process = await context.Process.RunAsync(new PluginProcessRequest
{
    FileName = "tool",
    Arguments = ["--version"],
    CaptureOutput = true
});
Assert.AreEqual("ok", process.StandardOutput);

await context.Clipboard.WriteTextAsync("copied");
Assert.AreEqual("copied", await context.Clipboard.ReadTextAsync());

await context.Files.WriteAsync("reports/latest.txt", "done"u8.ToArray());
CollectionAssert.AreEqual("done"u8.ToArray(), await context.Files.ReadAsync("reports/latest.txt"));

context.Accounts.AddProvider(new PluginAccountProviderInfo("offline", "离线账户", null));
context.Downloads.AddSource(new PluginDownloadSourceInfo(
    "official",
    "官方源",
    new Uri("https://example.invalid/"),
    "Metadata"));

context.LaunchModifications.Register(new PluginLaunchModification(
    "add-demo-flag",
    request => request with { GameArguments = request.GameArguments.Concat(["--demo"]).ToArray() }));
```

这些测试替身不会启动真实进程、访问系统剪贴板或读取真实账户；它们只记录请求并返回内存数据，适合做插件逻辑回归。文件服务会把路径限制在测试插件数据目录内。

## 测试设置页 Capability

```csharp
InMemoryPluginSettingsPageCapability pages = new();
context.TestCapabilities.Add<IPluginSettingsPageCapability>(pages);

await plugin.InitializeAsync(context, CancellationToken.None);

Assert.AreEqual(1, pages.Pages.Count);
Assert.AreEqual("dev.example.plugin.settings", pages.Pages[0].Id);
```

## 验证释放

```csharp
TestPluginCommandService commands = new();
TestPluginContext context = new(descriptor, new PluginApiVersion(0, 2));
context.TestServices.Add<IPluginCommandService>(commands);

await plugin.InitializeAsync(context, CancellationToken.None);
Assert.AreEqual(1, commands.Commands.Count);

await context.DisposeAsync();
Assert.AreEqual(0, commands.Commands.Count);
```

这能发现忘记 `context.Lifetime.Track(...)` 的注册泄漏。

## 推荐测试矩阵

- required 服务齐全时成功初始化；
- optional 服务缺失时正常降级；
- 取消 Token 时任务结束；
- 重复命令/页面 ID 会失败；
- 设置默认值和更新值正确；
- UI Surface 存在/缺失两种路径；
- Context 释放后注册项消失；
- Manifest 通过 `PluginManifestValidator.ParseAndValidate`；
- Release 构建实际生成 `.pnp`。

仓库中的 SDK 测试和 [`examples/HelloPlugin`](https://github.com/MuXue1230-owo/PCL-N-Plugin-SDK/tree/main/examples/HelloPlugin) 可作为基线。