# NuGet 包

> 当前公开版本：`0.2.1`

所有包 ID 使用 `PCLN.Plugin.*`。为保持源码和二进制兼容，程序集名与命名空间仍使用 `PCL.N.Plugin.*`，因此 C# 代码继续写 `using PCL.N.Plugin;`。

| 包 | 用途 | 应放在哪里 |
|---|---|---|
| [`PCLN.Plugin.Abstractions`](https://www.nuget.org/packages/PCLN.Plugin.Abstractions) | 入口、生命周期、核心服务、Manifest 和声明式 UI 公共 ABI | 插件项目 |
| [`PCLN.Plugin.i18n`](https://www.nuget.org/packages/PCLN.Plugin.i18n) | 强类型本地化文本、设置页本地化 capability 与宿主本地化辅助 API | 所有插件项目（必需） |
| [`PCLN.Plugin.UI`](https://www.nuget.org/packages/PCLN.Plugin.UI) | UI Target、导航和插件页面元数据等稳定 UI 契约 | 需要导航或完整 UI 的插件项目 |
| [`PCLN.Plugin.UI.Avalonia`](https://www.nuget.org/packages/PCLN.Plugin.UI.Avalonia) | 受权限与 Host 能力控制的 Raw Avalonia、完整页面和窗口契约 | 明确需要 Avalonia 对象的插件项目 |
| [`PCLN.Plugin.Sdk`](https://www.nuget.org/packages/PCLN.Plugin.Sdk) | Manifest 验证、版本范围、Capability 辅助扩展 | 插件项目 |
| [`PCLN.Plugin.Analyzers`](https://www.nuget.org/packages/PCLN.Plugin.Analyzers) | PNPSDK001–010 编译诊断 | 插件项目，`PrivateAssets=all` |
| [`PCLN.Plugin.Testing`](https://www.nuget.org/packages/PCLN.Plugin.Testing) | 内存服务与生命周期测试宿主 | 仅测试项目 |
| [`PCLN.Plugin.Sdk.Build`](https://www.nuget.org/packages/PCLN.Plugin.Sdk.Build) | `.pnp` 打包、文件表、可复现构建和 GPG 签名 | 插件项目，`PrivateAssets=all` |

## 包依赖层级

- `PCLN.Plugin.i18n` 依赖 `PCLN.Plugin.Abstractions`，提供 `PclLocalizedString` 和强制本地化设置页契约。
- `PCLN.Plugin.UI` 依赖 `PCLN.Plugin.Abstractions` 与 `PCLN.Plugin.i18n`，不引入具体桌面 UI 框架。
- `PCLN.Plugin.UI.Avalonia` 依赖 `PCLN.Plugin.Abstractions`、`PCLN.Plugin.UI` 和 Avalonia 12。
- 只使用生命周期、命令、设置页描述或声明式 AXAML Slot 的插件不需要引用 `PCLN.Plugin.UI.Avalonia`。
- 引用 UI 包只提供编译期契约；运行时仍须通过服务版本与 `TryGet` 协商 Host 是否提供对应能力。

## 推荐项目引用

核心插件引用：

```xml
<ItemGroup>
  <PackageReference Include="PCLN.Plugin.Abstractions" Version="0.2.1" />
  <PackageReference Include="PCLN.Plugin.i18n" Version="0.2.1" />
  <PackageReference Include="PCLN.Plugin.Sdk" Version="0.2.1" PrivateAssets="all" />
  <PackageReference Include="PCLN.Plugin.Analyzers" Version="0.2.1" PrivateAssets="all" />
  <PackageReference Include="PCLN.Plugin.Sdk.Build" Version="0.2.1" PrivateAssets="all" />
  <AdditionalFiles Include="plugin.json" />
</ItemGroup>
```

需要稳定 UI Target、导航或页面元数据时，再加入：

```xml
<PackageReference Include="PCLN.Plugin.UI" Version="0.2.1" />
```

需要创建完整 Avalonia 页面、窗口或申请 Raw Avalonia访问时，再加入：

```xml
<PackageReference Include="PCLN.Plugin.UI.Avalonia" Version="0.2.1" />
```

`PCLN.Plugin.UI.Avalonia` 会传递所需的 UI契约和 Avalonia依赖，不要仅为了声明式 AXAML注入而引用它。

测试项目：

```xml
<PackageReference Include="PCLN.Plugin.Testing" Version="0.2.1" />
```

## 强制本地化

SDK 要求每个插件至少提供 `locales/zh-CN.json` 与 `locales/en-US.json`，且两个文件必须包含完全相同的键集合。所有用户可见 UI 与设置页文本必须使用：

```csharp
new PclLocalizedString("settings.title", "设置")
```

构造函数第二项是简体中文回退文本；实际显示由本体提供的 `IPluginLocalizationService` 按当前语言解析。`PclUiString`、`PluginSettingsPageDescriptor` 和旧设置页 capability 已标记为弃用，仅用于迁移旧插件。

## 为什么不能引用 PCL.Plugin？

`PCL.Plugin` 是 PCL N 内置的私有特权 Host，负责加载、安全、市场、恢复和桌面桥接。它可以随启动器重构，第三方插件对其建立编译依赖会破坏兼容和安全边界。

Analyzer 会阻止引用：

- `PCL.Application`
- `PCL.Desktop`
- `PCL.Plugin`
- 对应私有命名空间

## 版本锁定

alpha 阶段建议在仓库中显式固定完整版本，并让依赖更新通过单独提交进入：

```xml
<PclNPluginSdkVersion>0.2.1</PclNPluginSdkVersion>
```

```xml
<PackageReference Include="PCLN.Plugin.Abstractions" Version="$(PclNPluginSdkVersion)" />
```

不要使用 `0.1.*` 等浮动版本构建正式发布物，否则同一源码在不同时间可能得到不同 ABI 和包哈希。
