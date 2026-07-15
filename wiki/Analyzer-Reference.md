# Analyzer 参考

> SDK `0.1.0`

`PCLN.Plugin.Analyzers` 在编译期检查宿主边界、入口类型、生命周期、Manifest 和后台工作。运行时与打包器仍会重复执行安全验证。

| ID | 触发原因 | 修复方向 |
|---|---|---|
| PNPSDK001 | 引用了 `PCL.Application` | 改用公开 Abstractions 服务 |
| PNPSDK002 | 引用了 `PCL.Desktop` | 使用 UI Surface、命令或设置页 Capability |
| PNPSDK003 | 引用了私有 `PCL.Plugin` | 删除引用，只保留 `PCLN.Plugin.*` |
| PNPSDK004 | 插件入口类型无效 | 使用 public、非抽象、非泛型、有无参构造的 `IPclNPlugin` |
| PNPSDK005 | 注册句柄未跟踪 | 调用 `context.Lifetime.Track(...)` |
| PNPSDK006 | 使用私有宿主命名空间 | 替换为公开 DTO/接口 |
| PNPSDK007 | `plugin.json` 缺少必需字段 | 按 Schema 补齐字段 |
| PNPSDK008 | 缺少 API 版本范围 | 添加 `api.minimum` 和 `api.maximumExclusive` |
| PNPSDK009 | AXAML 路径或高风险 UI 声明无效 | 使用 `ui/*.axaml` 安全路径并声明权限 |
| PNPSDK010 | 创建未托管后台工作 | 使用 `IPluginTaskService` 或跟踪可释放资源 |

## 启用 Manifest 诊断

```xml
<ItemGroup>
  <AdditionalFiles Include="plugin.json" />
</ItemGroup>
```

如果 `plugin.json` 不在项目根目录：

```xml
<AdditionalFiles Include="manifest/plugin.json" Link="plugin.json" />
<PclNPluginManifest>$(MSBuildProjectDirectory)/manifest/plugin.json</PclNPluginManifest>
```

## 示例：PNPSDK005

错误：

```csharp
commands.Register(descriptor);
```

正确：

```csharp
context.Lifetime.Track(commands.Register(descriptor));
```

## 示例：PNPSDK010

错误：

```csharp
_ = Task.Run(() => RefreshForeverAsync(CancellationToken.None));
```

正确：

```csharp
IPluginTaskService tasks = context.Services.Require<IPluginTaskService>();
context.Lifetime.Track(tasks.SchedulePeriodic(
    "dev.example.refresh",
    TimeSpan.FromMinutes(5),
    RefreshAsync));
```

不要为了通过编译而全局禁用 PNPSDK 规则。若确需例外，先确认资源可以随生命周期停止，并在最小代码范围内附带解释。
