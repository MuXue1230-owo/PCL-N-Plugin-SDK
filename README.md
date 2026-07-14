# PCL N Plugin SDK

PCL N 第三方插件的公开契约、开发工具、分析器、测试宿主和 `.pnp` 构建工具。

> 当前版本：`0.1.0-alpha.4`。1.0 前公共 API 仍可能调整。

## 包

| NuGet 包 | 用途 |
|---|---|
| `PCLN.Plugin.Abstractions` | 插件入口、生命周期、服务、Manifest 和 UI 公共 ABI |
| `PCLN.Plugin.Sdk` | Manifest 验证、SemVer 范围和开发辅助 API |
| `PCLN.Plugin.Analyzers` | `PNPSDK001–010` 编译诊断 |
| `PCLN.Plugin.Testing` | 内存服务与生命周期测试宿主 |
| `PCLN.Plugin.Sdk.Build` | 可复现 `.pnp` 打包和 OpenPGP/GPG 签名 |

NuGet 包 ID 使用 `PCLN.Plugin.*`；为保持源码和二进制兼容，程序集名与命名空间仍使用 `PCL.N.Plugin.*`。

`PCL.Plugin` 是 PCL-N 的私有特权 Host，不属于公共 SDK。第三方插件不得引用 `PCL.Application`、`PCL.Desktop` 或 `PCL.Plugin`。

## 快速开始

安装 .NET 10 SDK，然后参考 `examples/HelloPlugin`。示例展示：

- `IPclNPlugin` 生命周期；
- 命令和设置页注册；
- Manifest UI Patch 声明；
- 无代码隐藏的声明式 AXAML；
- Release 构建生成平台无关的 AnyCPU `.pnp`，并支持 `runtimes/<rid>/native/` 多架构原生资产。

```powershell
dotnet restore PCL-N-Plugin-SDK.slnx
dotnet build PCL-N-Plugin-SDK.slnx -c Release -warnaserror
dotnet test PCL-N-Plugin-SDK.slnx -c Release --no-build
```

正式插件包必须签名。开发构建可以显式设置：

```xml
<PclNPluginSign>false</PclNPluginSign>
```

未签名输出会产生警告，不得作为正式分发物。

## AXAML UI

Manifest 的 UI operation 可以引用包内 `ui/*.axaml`：

```json
{
  "id": "hello-panel",
  "kind": "inject",
  "slot": "primary-actions.after",
  "axaml": "ui/HelloPanel.axaml",
  "command": "dev.muxue.hello.say-hello"
}
```

首版只支持声明式 AXAML：禁止 `x:Class`、代码隐藏和 PCL-N 私有 CLR 命名空间。行为通过公开命令 ID 和 Host 提供的公开绑定上下文连接。

## 文档

版本化 Wiki 源文件位于 `wiki/`，发布工作流会同步到 [GitHub Wiki](https://github.com/MuXue1230-owo/PCL-N-Plugin-SDK/wiki)。同一套详细文档也发布到 [docs.pcln.top](https://docs.pcln.top/plugin-sdk/)。

- [从零创建第一个插件](https://docs.pcln.top/plugin-sdk/Getting-Started)
- [桌面端安装与调试](https://docs.pcln.top/plugin-sdk/Desktop-Installation-and-Debugging)
- [十个实战案例](https://docs.pcln.top/plugin-sdk/Examples-Cookbook)
- [完整 Manifest 参考](https://docs.pcln.top/plugin-sdk/Plugin-Manifest)

Manifest Schema 位于 `schemas/plugin.schema.json`。

## CI 与发布

CI 在 Windows、Linux 和 macOS 上构建与测试，并验证五个 NuGet 包、示例 `.pnp`、AXAML 资源和可复现输出。`sdk-v*` Tag 经 `nuget-production` 环境审批后发布 NuGet 并同步 Wiki。

Licensed under the Apache License 2.0.
