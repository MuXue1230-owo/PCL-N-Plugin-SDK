# NuGet 包

> 当前公开版本：`0.1.0-alpha.5`

所有包 ID 使用 `PCLN.Plugin.*`。为保持源码和二进制兼容，程序集名与命名空间仍使用 `PCL.N.Plugin.*`，因此 C# 代码继续写 `using PCL.N.Plugin;`。

| 包 | 用途 | 应放在哪里 |
|---|---|---|
| [`PCLN.Plugin.Abstractions`](https://www.nuget.org/packages/PCLN.Plugin.Abstractions) | 入口、生命周期、服务、Manifest 和 UI 公共 ABI | 插件项目 |
| [`PCLN.Plugin.Sdk`](https://www.nuget.org/packages/PCLN.Plugin.Sdk) | Manifest 验证、版本范围、Capability 辅助扩展 | 插件项目 |
| [`PCLN.Plugin.Analyzers`](https://www.nuget.org/packages/PCLN.Plugin.Analyzers) | PNPSDK001–010 编译诊断 | 插件项目，`PrivateAssets=all` |
| [`PCLN.Plugin.Testing`](https://www.nuget.org/packages/PCLN.Plugin.Testing) | 内存服务与生命周期测试宿主 | 仅测试项目 |
| [`PCLN.Plugin.Sdk.Build`](https://www.nuget.org/packages/PCLN.Plugin.Sdk.Build) | `.pnp` 打包、文件表、可复现构建和 GPG 签名 | 插件项目，`PrivateAssets=all` |

## 推荐项目引用

```xml
<ItemGroup>
  <PackageReference Include="PCLN.Plugin.Abstractions" Version="0.1.0-alpha.5" />
  <PackageReference Include="PCLN.Plugin.Sdk" Version="0.1.0-alpha.5" PrivateAssets="all" />
  <PackageReference Include="PCLN.Plugin.Analyzers" Version="0.1.0-alpha.5" PrivateAssets="all" />
  <PackageReference Include="PCLN.Plugin.Sdk.Build" Version="0.1.0-alpha.5" PrivateAssets="all" />
  <AdditionalFiles Include="plugin.json" />
</ItemGroup>
```

测试项目：

```xml
<PackageReference Include="PCLN.Plugin.Testing" Version="0.1.0-alpha.5" />
```

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
<PclNPluginSdkVersion>0.1.0-alpha.5</PclNPluginSdkVersion>
```

```xml
<PackageReference Include="PCLN.Plugin.Abstractions" Version="$(PclNPluginSdkVersion)" />
```

不要使用 `0.1.*` 等浮动版本构建正式发布物，否则同一源码在不同时间可能得到不同 ABI 和包哈希。
