# 学习路线

> 适用于 SDK `0.1.0` 与 PCL.Plugin `v0.12.0`。

本页把零散的 API 参考整理成一条可以照着完成的开发路线。第一次开发插件时，建议依次完成前四个阶段；准备公开发布时，再完成第五阶段。

## 先选择你的目标

| 你想实现的功能 | 建议先读 | 完成标准 |
|---|---|---|
| 创建第一个可安装插件 | [从零完成第一个插件](Tutorial-First-Plugin) | 得到一个能安装、启用并执行命令的 `.pnp` |
| 保存设置、读取实例、执行后台任务 | [服务、设置与后台任务实战](Tutorial-Services-and-Settings) | 功能在可选服务缺失时仍能安全降级 |
| 添加设置页或启动页内容 | [UI 扩展实战](Tutorial-UI-Extension) | UI 通过公开 Capability、Surface 和 Slot 接入 |
| 使用其他插件或第三方库 | [依赖与打包实战](Tutorial-Dependencies-and-Packaging) | 前置、托管依赖和原生资产都声明正确 |
| 建立测试与发布流水线 | [测试、调试与发布实战](Tutorial-Test-Debug-Release) | 测试通过、包已签名、版本可以追溯 |

## 阶段一：做出最小插件

1. 安装 .NET 10 SDK。
2. 创建 `net10.0` 类库并安装 `PCLN.Plugin.*` 包。
3. 实现 `IPclNPlugin`。
4. 编写 `plugin.json`。
5. 构建并在 PCL N 开发者模式中安装 `.pnp`。

先完成 [快速开始](Getting-Started)，再跟随 [从零完成第一个插件](Tutorial-First-Plugin) 补上命令、日志、通知和验收步骤。

## 阶段二：理解边界

插件不是启动器内部模块。它只能依赖公开 SDK，并通过 `IPluginContext` 访问宿主能力：

```text
插件代码
  ├─ PCLN.Plugin.Abstractions：接口、DTO、生命周期
  ├─ PCLN.Plugin.Sdk：开发辅助 API
  ├─ context.Services：通知、设置、命令、任务、实例、UI
  └─ context.Capabilities：宿主可选能力

禁止引用
  ├─ PCL.Application
  ├─ PCL.Desktop
  └─ PCL.Plugin（宿主私有运行时）
```

随后阅读：

- [架构与边界](Architecture-and-Boundaries)
- [生命周期与注册项](Lifecycle-and-Registrations)
- [服务](Services)
- [兼容与废弃](Compatibility-and-Deprecation)

## 阶段三：实现真实功能

把功能拆成三层：

1. 核心逻辑使用普通 C# 类，尽量不依赖 PCL N。
2. 插件入口负责从 `context.Services` 取得宿主服务并注册命令、任务或页面。
3. Manifest 声明运行前必须验证的权限、服务、前置和 UI 兼容范围。

这样核心逻辑可以单元测试，宿主接入可以使用 `PCLN.Plugin.Testing` 验证，Manifest 则由 Analyzer 和构建器检查。

## 阶段四：加入 UI 和兼容策略

只有在确实需要界面时再接入 UI：

- 插件设置优先使用设置页 Capability；
- 在现有页面增加内容使用声明式 AXAML 和公开 Slot；
- 不读取 Visual Tree，不依赖控件名、索引或本地化文本；
- 装饰性 UI 使用 `required: false`，Surface 不可用时保留核心功能。

参见 [UI 扩展实战](Tutorial-UI-Extension) 和 [UI Surface 与 Slot](UI-Surfaces-and-Slots)。

## 阶段五：测试并发布

发布前应形成固定流水线：

```text
restore → analyzer/build → test → 生成 .pnp → 检查包内容
        → 桌面端安装/启用/停用/升级 → OpenPGP 签名 → 发布
```

不要覆盖已经发布的版本号；Manifest、项目版本、Git Tag 和商店版本必须一致。完整操作见 [测试、调试与发布实战](Tutorial-Test-Debug-Release)。

## 遇到问题时怎么查

1. 构建期错误先查 [Analyzer 参考](Analyzer-Reference)。
2. `.pnp` 生成和包内容问题查 [构建 `.pnp`](Building-PNP-Packages)。
3. 安装、签名、兼容或启动失败查 [故障排查](Troubleshooting)。
4. 不确定设计边界时查 [FAQ](FAQ)。
