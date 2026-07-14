# 测试、调试与发布实战

> 适用于 SDK `0.1.0-alpha.5`。目标是让同一个提交可重复通过测试、生成 `.pnp` 并发布。

推荐把验证分成三层：

```text
内存宿主测试（快）
  → Release 打包检查（验证 Manifest 和文件）
  → PCL N 桌面端验收（验证真实 Host、UI 和升级）
```

## 1. 创建测试项目

在解决方案根目录运行：

```powershell
dotnet new mstest -n PclNToolbox.Plugin.Tests --framework net10.0
dotnet sln add .\PclNToolbox.Plugin.Tests\PclNToolbox.Plugin.Tests.csproj
dotnet add .\PclNToolbox.Plugin.Tests package PCLN.Plugin.Testing --version 0.1.0-alpha.5
dotnet add .\PclNToolbox.Plugin.Tests reference .\PclNToolbox.Plugin\PclNToolbox.Plugin.csproj
```

`PCLN.Plugin.Testing` 只进入测试项目，不能成为插件运行依赖。

## 2. 测试命令注册和可选通知

```csharp
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin;
using PCL.N.Plugin.Testing;
using PclNToolbox.Plugin;

namespace PclNToolbox.Plugin.Tests;

[TestClass]
public sealed class ToolboxPluginTests
{
    private static PluginDescriptor CreateDescriptor() => new(
        new PluginId("dev.example.toolbox"),
        "PCL N Toolbox",
        PluginVersion.Parse("0.1.0"));

    [TestMethod]
    public async Task Initialize_registers_scan_command()
    {
        await using TestPluginContext context = new(
            CreateDescriptor(),
            new PluginApiVersion(0, 2));

        TestPluginCommandService commands = new();
        TestPluginSettingsStore settings = new();
        TestPluginTaskService tasks = new();
        TestPluginInstanceReadService instances = new(
        [
            new PluginInstanceInfo("survival", "生存世界", @"D:\Games\survival")
        ]);

        context.TestServices.Add<IPluginCommandService>(commands);
        context.TestServices.Add<IPluginSettingsStore>(settings);
        context.TestServices.Add<IPluginTaskService>(tasks);
        context.TestServices.Add<IPluginInstanceReadService>(instances);

        ToolboxPlugin plugin = new();
        await plugin.InitializeAsync(context, CancellationToken.None);

        Assert.IsTrue(commands.Commands.Any(
            command => command.Id == "dev.example.toolbox.scan-instances"));
    }

    [TestMethod]
    public async Task Scan_works_without_optional_notifications()
    {
        await using TestPluginContext context = new(
            CreateDescriptor(),
            new PluginApiVersion(0, 2));

        TestPluginCommandService commands = new();
        context.TestServices.Add<IPluginCommandService>(commands);
        context.TestServices.Add<IPluginSettingsStore>(new TestPluginSettingsStore());
        context.TestServices.Add<IPluginTaskService>(new TestPluginTaskService());
        context.TestServices.Add<IPluginInstanceReadService>(
            new TestPluginInstanceReadService([]));

        ToolboxPlugin plugin = new();
        await plugin.InitializeAsync(context, CancellationToken.None);
        await commands.InvokeAsync("dev.example.toolbox.scan-instances");

        Assert.IsTrue(context.Logger.Messages.Any(
            message => message.Contains("实例扫描完成", StringComparison.Ordinal)));
    }
}
```

测试使用的 Windows 路径只是 DTO 数据，不要求真实存在。若测试 API 名称随预发布 SDK 调整，以 [测试插件](Testing-Plugins) 和仓库内测试为准。

## 3. 测试生命周期释放

插件停用后，命令、页面和任务必须消失：

```csharp
TestPluginContext context = new(
    CreateDescriptor(),
    new PluginApiVersion(0, 2));
TestPluginCommandService commands = new();
context.TestServices.Add<IPluginCommandService>(commands);
// 同样添加插件所需的其他 required 测试服务。

ToolboxPlugin plugin = new();
await plugin.InitializeAsync(context, CancellationToken.None);
Assert.IsTrue(commands.Commands.Count > 0);

await context.DisposeAsync();
Assert.AreEqual(0, commands.Commands.Count);
```

如果最后一个断言失败，通常是某个 `Register`、`Run`、`SchedulePeriodic` 或 Capability 注册没有交给 `context.Lifetime.Track`。

## 4. 本地质量门槛

每次准备提交前运行：

```powershell
dotnet restore
dotnet build -c Release -warnaserror
dotnet test -c Release --no-build
```

然后检查生成的包：

```powershell
tar -tf .\PclNToolbox.Plugin\bin\Release\net10.0\dev.example.toolbox-0.1.0.pnp
Get-FileHash .\PclNToolbox.Plugin\bin\Release\net10.0\dev.example.toolbox-0.1.0.pnp -Algorithm SHA256
```

Analyzer 警告应作为发布阻断项处理，不要长期使用 `NoWarn` 隐藏 Manifest、生命周期或边界问题。

## 5. 桌面端调试循环

1. 使用 SDK 本机开发密钥签名的 Debug/Release 包时，在 PCL N 完成开发者模式授权。
2. 安装 `.pnp`，启用插件，记录本次插件版本和包哈希。
3. 验证核心命令、设置、后台任务和 UI。
4. 停用插件，确认命令、任务和 UI 都已清理。
5. 查看插件独立日志目录，不只看界面提示。
6. 修改代码后重新构建；需要覆盖安装时先停用旧插件。
7. 发布前用正式签名包重复一次安装、启用、停用和升级测试。

至少覆盖：

- required 服务缺失或版本不匹配时出现可理解错误；
- optional 服务缺失时插件仍可用；
- 任务在停用时及时取消；
- UI 在小窗口、最大化、浅色/深色和 Linux 字体环境下正常；
- required 插件前置缺失、禁用和版本不匹配三种情况；
- 从上一稳定版本升级后设置和数据仍可读取。

## 6. GitHub Actions 示例

在 `.github/workflows/release.yml` 创建：

```yaml
name: Release plugin

on:
  push:
    tags: ['v*']

permissions:
  contents: write

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore
      - run: dotnet build -c Release --no-restore -warnaserror
      - run: dotnet test -c Release --no-build
      - name: Import signing key
        env:
          PNP_GPG_PRIVATE_KEY: ${{ secrets.PNP_GPG_PRIVATE_KEY }}
        run: printf '%s' "$PNP_GPG_PRIVATE_KEY" | gpg --batch --import
      - name: Build signed package
        run: >-
          dotnet build PclNToolbox.Plugin/PclNToolbox.Plugin.csproj
          -c Release --no-restore
          -p:PclNPluginSign=true
          -p:PclNPluginOutputDirectory=${{ github.workspace }}/artifacts/
      - uses: softprops/action-gh-release@v2
        with:
          files: artifacts/*.pnp
```

私钥只放 GitHub Actions Secret，不写入仓库、日志、`.pnp` 或构建缓存。公钥可以随包导出。

## 7. 发布版本

发布前确认项目和 Manifest 都是目标版本，例如 `0.1.0`：

```powershell
git status --short
git tag -s v0.1.0 -m "PCL N Toolbox 0.1.0"
git push origin v0.1.0
```

然后：

1. 确认 Actions 测试与签名构建成功。
2. 下载 Release 的 `.pnp` 并核对 SHA-256。
3. 在干净的 PCL N 环境安装 Release 资产，而不是本机另一次构建的文件。
4. 上传同一个 `.pnp` 到 [PCL N 插件中心](https://pcln.top/)。
5. 填写变更说明、权限、前置、兼容范围、许可证和支持链接。
6. 审核通过后再次从商店安装验证。

已经发布的版本不可覆盖。任何二进制变化都要提升版本号，否则用户、商店和审计记录会看到同版本不同哈希。

## 发布验收清单

- 构建、测试和 Analyzer 全部通过；
- 项目版本、Manifest、Tag 和商店版本一致；
- 正式 OpenPGP 签名有效，指纹属于发布者；
- Release 与商店 `.pnp` 哈希一致；
- required 前置已经公开可安装；
- Manifest 权限理由清晰、兼容范围准确；
- 安装、启用、停用、升级和卸载都已验证；
- 公开页面包含许可证、源码/支持地址和变更说明；
- 没有私钥、Token、用户数据、绝对路径或宿主私有程序集进入包。

遇到失败时先记录插件版本、PCL N 版本、平台、包哈希和完整日志，再查 [故障排查](Troubleshooting)。
