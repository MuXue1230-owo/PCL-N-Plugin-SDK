# 快速开始

> 适用于 SDK `0.1.0-alpha.4`。本页从空目录开始创建一个可以打包为 `.pnp` 的插件。

## 1. 准备环境

你需要：

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)；
- Visual Studio、JetBrains Rider 或 VS Code；
- 包含 PCL.Plugin 运行时的新版本 PCL N；
- 正式发布时还需要 GnuPG 2.x，开发阶段可以先不签名。

确认 SDK：

```powershell
dotnet --version
```

输出应为 `10.0.x`。

## 2. 创建项目

```powershell
mkdir HelloPclN
cd HelloPclN
dotnet new classlib --framework net10.0
dotnet add package PCLN.Plugin.Abstractions --version 0.1.0-alpha.4
dotnet add package PCLN.Plugin.Sdk --version 0.1.0-alpha.4
dotnet add package PCLN.Plugin.Analyzers --version 0.1.0-alpha.4
dotnet add package PCLN.Plugin.Sdk.Build --version 0.1.0-alpha.4
```

将项目文件整理为：

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>14.0</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <Version>0.1.0</Version>
    <PclNPluginId>dev.example.hello</PclNPluginId>
    <!-- 仅用于本地开发；正式发布必须改为 true 并配置 GPG。 -->
    <PclNPluginSign>false</PclNPluginSign>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="PCLN.Plugin.Abstractions" Version="0.1.0-alpha.4" />
    <PackageReference Include="PCLN.Plugin.Sdk" Version="0.1.0-alpha.4" PrivateAssets="all" />
    <PackageReference Include="PCLN.Plugin.Analyzers" Version="0.1.0-alpha.4" PrivateAssets="all" />
    <PackageReference Include="PCLN.Plugin.Sdk.Build" Version="0.1.0-alpha.4" PrivateAssets="all" />
    <AdditionalFiles Include="plugin.json" />
  </ItemGroup>
</Project>
```

`PclNPluginId` 必须与 Manifest 的 `id` 完全相同。`plugin.json` 加入 `AdditionalFiles` 后，Analyzer 才能在编译期检查 Manifest。

## 3. 实现插件入口

删除模板生成的 `Class1.cs`，新建 `HelloPlugin.cs`：

```csharp
using PCL.N.Plugin;

namespace HelloPclN;

public sealed class HelloPlugin : IPclNPlugin
{
    public ValueTask InitializeAsync(
        IPluginContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        context.Logger.Info($"{context.Plugin.Name} 已加载，API {context.ApiVersion}。");

        if (context.Services.TryGet<IPluginNotificationService>(out var notifications))
            notifications.ShowInformation("Hello from PCL N plugin!");

        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken)
    {
        return ValueTask.CompletedTask;
    }
}
```

入口类型必须：

- 是 `public`、非抽象、非泛型类；
- 实现 `IPclNPlugin`；
- 有公开的无参数构造函数；
- 与 `plugin.json` 的 `entryPoint.type` 完全一致。

## 4. 编写 `plugin.json`

在项目根目录新建：

```json
{
  "formatVersion": 1,
  "manifestVersion": 1,
  "id": "dev.example.hello",
  "name": "Hello PCL N",
  "version": "0.1.0",
  "channel": "alpha",
  "summary": "第一个 PCL N 插件。",
  "publisher": {
    "id": "github:your-name",
    "namespace": "dev.example"
  },
  "license": "MIT",
  "entryPoint": {
    "assembly": "lib/net10.0/HelloPclN.dll",
    "type": "HelloPclN.HelloPlugin"
  },
  "api": {
    "minimum": "0.1",
    "maximumExclusive": "1.0"
  },
  "host": {
    "minimumVersion": "0.1.0"
  },
  "services": {
    "optional": {
      "pcl.notifications": ">=0.1 <1.0"
    }
  },
  "permissions": [
    {
      "id": "pcl.notifications",
      "reason": "在插件加载后显示一条欢迎提示。"
    }
  ],
  "signing": {
    "fingerprint": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
  }
}
```

开发阶段可以暂用占位指纹并关闭签名。正式发布前必须替换为完整 OpenPGP 指纹并启用签名。参见 [OpenPGP 签名](OpenPGP-Signing)。

## 5. 构建 `.pnp`

```powershell
dotnet restore
dotnet build -c Release
```

成功后会生成：

```text
bin/Release/net10.0/dev.example.hello-0.1.0.pnp
```

`.pnp` 是一个可复现 ZIP 容器，包含 Manifest、入口程序集、私有依赖、文件表和签名元数据。不要手工把 `PCL.N.Plugin.Abstractions.dll` 塞进包内，构建器会把宿主共享程序集排除。

## 6. 安装和验证

1. 打开 PCL N 的“设置”。
2. 在“插件”分组进入“已安装”。
3. 点击“安装 `.pnp`”并选择刚生成的文件。
4. 未签名开发包需要先在“启动器 → 开发者模式”完成开发者授权，再显式允许未签名插件。
5. 启用插件，检查提示和插件日志。

完整说明见 [桌面端安装与调试](Desktop-Installation-and-Debugging)。

## 7. 下一步

- 按完整流程继续：[从零完成第一个插件](Tutorial-First-Plugin)
- 复制更多场景：[实战案例集](Examples-Cookbook)
- 保存设置、注册命令和后台任务：[服务、设置与后台任务实战](Tutorial-Services-and-Settings)
- 添加设置页或声明式 AXAML：[UI 扩展实战](Tutorial-UI-Extension)
- 编写自动化测试并准备发行：[测试、调试与发布实战](Tutorial-Test-Debug-Release)

仓库内的 [`examples/HelloPlugin`](https://github.com/MuXue1230-owo/PCL-N-Plugin-SDK/tree/main/examples/HelloPlugin) 是可以直接构建的完整示例。
