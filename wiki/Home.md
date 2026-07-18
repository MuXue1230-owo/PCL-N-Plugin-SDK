# PCL N Plugin SDK

> Applies to PCL N Plugin SDK 0.2.1.

> 适用于 SDK `0.2.1` 与 PCL.Plugin `v0.12.0`。当前仍是预发布版本，1.0 前公共 API 可能调整。

PCL N Plugin SDK 是 PCL N 第三方插件的公开开发入口。你可以用它创建 `.pnp` 插件、调用稳定宿主服务、向公开 UI Surface 添加界面，并在不启动 PCL N 的情况下完成自动化测试。

`PCL.Plugin` 是启动器内置的私有运行时，不是开发依赖。第三方插件只引用公开 NuGet 包，禁止引用 `PCL.Application`、`PCL.Desktop` 或 `PCL.Plugin`。

## 我该从哪里开始？

| 目标 | 推荐入口 |
|---|---|
| 不确定学习顺序 | [学习路线](Learning-Path) |
| 第一次创建插件 | [快速开始](Getting-Started) → [从零完成第一个插件](Tutorial-First-Plugin) |
| 在 PCL N 中安装和调试 | [桌面端安装与调试](Desktop-Installation-and-Debugging) |
| 直接查找可复制代码 | [实战案例集](Examples-Cookbook) |
| 编写完整 `plugin.json` | [Plugin Manifest](Plugin-Manifest) |
| 调用通知、设置、命令或后台任务 | [服务、设置与后台任务实战](Tutorial-Services-and-Settings) |
| 添加设置页或启动页面板 | [UI 扩展实战](Tutorial-UI-Extension) |
| 管理前置、DLL 和原生库 | [依赖与打包实战](Tutorial-Dependencies-and-Packaging) |
| 编写测试并正式发布 | [测试、调试与发布实战](Tutorial-Test-Debug-Release) |

## 完整开发流程

```text
创建 net10.0 类库
  → 安装 PCLN.Plugin.* NuGet 包
  → 实现 IPclNPlugin
  → 编写 plugin.json
  → dotnet build -c Release
  → 在 PCL N 桌面端安装并验证
  → 使用 PCLN.Plugin.Testing 编写测试
  → 使用 OpenPGP 正式签名
  → 发布不可变的 SemVer 版本
```

## 文档导航

### 入门教程

- [学习路线](Learning-Path)
- [快速开始](Getting-Started)
- [从零完成第一个插件](Tutorial-First-Plugin)
- [服务、设置与后台任务实战](Tutorial-Services-and-Settings)
- [UI 扩展实战](Tutorial-UI-Extension)
- [依赖与打包实战](Tutorial-Dependencies-and-Packaging)
- [测试、调试与发布实战](Tutorial-Test-Debug-Release)

### 核心概念

- [架构与边界](Architecture-and-Boundaries)
- [NuGet 包](NuGet-Packages)
- [身份与版本](Plugin-Identity-and-Versioning)
- [生命周期与注册项](Lifecycle-and-Registrations)
- [兼容与废弃](Compatibility-and-Deprecation)

### Manifest 与安全

- [Plugin Manifest](Plugin-Manifest)
- [依赖与兼容声明](Dependencies-and-Compatibility)
- [权限与安全](Permissions-and-Security)
- [Analyzer 参考](Analyzer-Reference)

### UI

- [UI Surface 与 Slot](UI-Surfaces-and-Slots)
- [UI Patch 与冲突](UI-Patches-and-Conflicts)

### 构建与发布

- [构建 `.pnp`](Building-PNP-Packages)
- [OpenPGP 签名](OpenPGP-Signing)
- [发布插件](Publishing-a-Plugin)
- [故障排查](Troubleshooting)
- [FAQ](FAQ)

## 官方资源

- [SDK 源码与完整示例](https://github.com/MuXue1230-owo/PCL-N-Plugin-SDK)
- [PCL N 插件商店](https://pcln.top/)
- [NuGet 上的 PCLN.Plugin.Abstractions](https://www.nuget.org/packages/PCLN.Plugin.Abstractions)
- [Manifest JSON Schema](https://github.com/MuXue1230-owo/PCL-N-Plugin-SDK/blob/main/schemas/plugin.schema.json)