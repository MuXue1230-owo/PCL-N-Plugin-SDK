# 从零完成第一个插件

> 适用于 SDK `0.1.0-alpha.5`。完成后你会得到一个能在 PCL N 中安装、注册命令并显示通知的 `.pnp`。

本教程使用 `dev.example.toolbox` 作为插件 ID。实际项目必须换成你长期控制的反向域名式 ID；插件发布后不要再修改它。

## 1. 创建解决方案和项目

```powershell
mkdir PclNToolbox
cd PclNToolbox
dotnet new sln -n PclNToolbox
dotnet new classlib -n PclNToolbox.Plugin --framework net10.0
dotnet sln add .\PclNToolbox.Plugin\PclNToolbox.Plugin.csproj
```

删除模板生成的 `Class1.cs`，然后把项目文件改为：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Version>0.1.0</Version>
    <PclNPluginId>dev.example.toolbox</PclNPluginId>
    <PclNPluginSign>false</PclNPluginSign>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="PCLN.Plugin.Abstractions" Version="0.1.0-alpha.5" />
    <PackageReference Include="PCLN.Plugin.Sdk" Version="0.1.0-alpha.5" PrivateAssets="all" />
    <PackageReference Include="PCLN.Plugin.Analyzers" Version="0.1.0-alpha.5" PrivateAssets="all" />
    <PackageReference Include="PCLN.Plugin.Sdk.Build" Version="0.1.0-alpha.5" PrivateAssets="all" />
    <AdditionalFiles Include="plugin.json" />
  </ItemGroup>
</Project>
```

这里有三个必须保持一致的值：

| 位置 | 值 |
|---|---|
| `PclNPluginId` | `dev.example.toolbox` |
| `plugin.json` 的 `id` | `dev.example.toolbox` |
| 命令 ID 前缀 | `dev.example.toolbox.*` |

`PrivateAssets="all"` 表示开发/构建工具不会作为插件运行依赖传递出去。`PCLN.Plugin.Abstractions` 由宿主共享，构建器也不会把它塞进 `.pnp`。

## 2. 实现插件入口

新建 `ToolboxPlugin.cs`：

```csharp
using PCL.N.Plugin;

namespace PclNToolbox.Plugin;

public sealed class ToolboxPlugin : IPclNPlugin
{
    public ValueTask InitializeAsync(
        IPluginContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Logger.Info($"正在初始化 {context.Plugin.Name} {context.Plugin.Version}。");

        IPluginCommandService commands =
            context.Services.Require<IPluginCommandService>();

        IPluginRegistration registration = commands.Register(
            new PluginCommandDescriptor(
                "dev.example.toolbox.hello",
                "Toolbox：打招呼",
                _ =>
                {
                    if (context.Services.TryGet<IPluginNotificationService>(
                            out var notifications))
                    {
                        notifications.ShowInformation("Toolbox 插件运行正常。");
                    }
                    else
                    {
                        context.Logger.Info("Toolbox 插件运行正常（通知服务不可用）。");
                    }

                    return Task.CompletedTask;
                },
                description: "验证插件命令与通知服务。",
                icon: "lucide/wrench"));

        context.Lifetime.Track(registration);
        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
```

关键点：

- `Require<IPluginCommandService>()` 对应必需服务；宿主缺少它时插件不应继续加载。
- `TryGet<IPluginNotificationService>()` 对应可选服务；缺失时降级为日志。
- `context.Lifetime.Track(...)` 让宿主在停用插件时自动注销命令。
- `InitializeAsync` 不应执行长时间文件扫描或网络请求；长任务应交给 `IPluginTaskService`。

## 3. 编写 Manifest

在项目根目录新建 `plugin.json`：

```json
{
  "formatVersion": 1,
  "manifestVersion": 1,
  "id": "dev.example.toolbox",
  "name": "PCL N Toolbox",
  "version": "0.1.0",
  "channel": "alpha",
  "summary": "用于学习 PCL N 插件开发的工具箱。",
  "publisher": {
    "id": "github:your-name",
    "namespace": "dev.example"
  },
  "license": "MIT",
  "entryPoint": {
    "assembly": "lib/net10.0/PclNToolbox.Plugin.dll",
    "type": "PclNToolbox.Plugin.ToolboxPlugin"
  },
  "api": {
    "minimum": "0.1",
    "maximumExclusive": "1.0"
  },
  "host": {
    "minimumVersion": "0.1.0"
  },
  "services": {
    "required": {
      "pcl.commands": ">=0.1 <1.0"
    },
    "optional": {
      "pcl.notifications": ">=0.1 <1.0"
    }
  },
  "permissions": [
    {
      "id": "pcl.notifications",
      "reason": "在用户执行示例命令后显示结果。"
    }
  ],
  "signing": {
    "fingerprint": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
  },
  "categories": ["tools"],
  "tags": ["tutorial", "toolbox"]
}
```

开发阶段可以设置 `<PclNPluginSign>false</PclNPluginSign>`，SDK 会自动创建本机开发密钥、改写完整指纹并签名。正式发布时必须替换为你自己的完整 OpenPGP 指纹，同时设置 `<PclNPluginSign>true</PclNPluginSign>`。

## 4. 构建并检查产物

```powershell
dotnet restore
dotnet build -c Release
```

产物位于：

```text
PclNToolbox.Plugin/bin/Release/net10.0/dev.example.toolbox-0.1.0.pnp
```

`.pnp` 是 ZIP 容器，可以只读检查：

```powershell
tar -tf .\PclNToolbox.Plugin\bin\Release\net10.0\dev.example.toolbox-0.1.0.pnp
```

至少应看到：

```text
plugin.json
lib/net10.0/PclNToolbox.Plugin.dll
META-INF/pnp.files.json
META-INF/pnp.signed.json
```

不应看到 `PCL.N.Plugin.Abstractions.dll`、`PCL.Application.dll`、`PCL.Desktop.dll` 或 `PCL.Plugin.dll`。

## 5. 在桌面端安装

1. 打开 PCL N 的“设置 → 启动器”，启用开发者模式并完成开发者授权。
2. 进入“设置 → 插件 → 已安装”。
3. 点击“安装 `.pnp`”，选择刚生成的包。
4. 确认这是由本机开发密钥签名的包并启用插件；未签名包不会被接受。
5. 在命令入口执行“Toolbox：打招呼”。
6. 检查通知；若宿主未提供通知服务，则检查插件日志。

每次重新构建后都应先停用旧版本，再覆盖安装或提升版本号。完整桌面端流程见 [桌面端安装与调试](Desktop-Installation-and-Debugging)。

## 6. 验收清单

- Release 构建没有 Analyzer 警告；
- `.pnp` 文件名中的 ID、版本与 Manifest 一致；
- 插件能启用，也能正常停用；
- 命令只出现一次，重复启用不会累积注册；
- 有通知服务时显示通知，没有时写入日志；
- 包内没有宿主私有程序集；
- 插件日志中没有未处理异常。

## 常见错误

| 现象 | 优先检查 |
|---|---|
| 找不到入口类型 | `entryPoint.type` 是否包含完整命名空间，类是否为 `public` |
| 构建没有生成 `.pnp` | 是否引用 `PCLN.Plugin.Sdk.Build`，是否设置 `PclNPluginId` |
| Manifest Analyzer 不工作 | `plugin.json` 是否加入 `AdditionalFiles` |
| 插件启用后命令重复 | 注册结果是否交给 `context.Lifetime.Track` |
| 安装时提示签名无效 | 本机开发密钥包是否由当前 SDK 重新构建、包是否被二次修改 |

下一步进入 [服务、设置与后台任务实战](Tutorial-Services-and-Settings)，把示例扩展成会保存配置和读取实例的真实功能。
