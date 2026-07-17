# UI Surface 与 Slot

> Applies to PCL N Plugin SDK 0.2.0.

> SDK `0.2.0`；Surface 目录以当前 PCL.Plugin `v0.12.0` 为准。

Surface 是宿主发布的稳定 UI 边界，Target 是 Manifest 中引用的 Surface ID，Slot 是稳定插入点。插件不能依赖控件类名、XAML Name、本地化文本或 Visual Tree 索引。

## 当前公开 Surface

| Surface | 版本 | 操作 | Slot |
|---|---:|---|---|
| `pcl.window.main` | `1.0` | `observe` | 无 |
| `pcl.navigation.main` | `1.0` | `observe`、`inject` | `items.after-download` |
| `pcl.page.launch` | `3.1` | `observe`、`inject`、`modify`、`replace`、`wrap` | `primary-actions.launch-button`、`primary-actions.after` |
| `pcl.page.settings` | `1.0` | `observe`、`inject` | `sidebar.after-plugin` |

Surface 会独立版本化。插件应声明经过测试的范围，而不是写死当前产品版本。

## 先查询能力

```csharp
IPluginUiSurfaceRegistry ui = context.Services.Require<IPluginUiSurfaceRegistry>();

bool supported = ui.SupportsSlot(
    "pcl.page.launch",
    "primary-actions.after",
    ">=3.0 <4.0",
    PluginUiOperation.Inject);
```

运行时会再次验证 Manifest；代码查询适合为可选 UI 提供降级路径。

## 声明式 AXAML 注入

Manifest：

```json
{
  "target": "pcl.page.launch",
  "surface": ">=3.0 <4.0",
  "access": ["inject"],
  "operations": [
    {
      "id": "hello-panel",
      "kind": "inject",
      "slot": "primary-actions.after",
      "axaml": "ui/HelloPanel.axaml",
      "command": "dev.example.hello",
      "priority": 0,
      "required": false,
      "fallback": "disable-feature"
    }
  ]
}
```

AXAML：

```xml
<Border xmlns="https://github.com/avaloniaui"
        Padding="12"
        CornerRadius="8">
  <StackPanel Spacing="6">
    <TextBlock FontWeight="SemiBold" Text="Hello Plugin" />
    <TextBlock Text="由插件提供的声明式面板。" TextWrapping="Wrap" />
    <Button Content="执行"
            Command="{Binding Commands[dev.example.hello]}" />
  </StackPanel>
</Border>
```

## AXAML 安全规则

- 禁止 `x:Class` 和代码隐藏；
- 禁止引用 `PCL.Application`、`PCL.Desktop`、`PCL.Plugin` 命名空间；
- 资源路径必须在 Manifest 中提前声明；
- 行为通过公开命令 ID 和宿主绑定上下文连接；
- 不要假设任意 Avalonia 类型、MarkupExtension 或资源 URI 都被允许。

## 代码注册 Slot 意图

`IPluginUiSurfaceCapability` 用于注册贡献意图：

```csharp
if (context.Capabilities.TryGet<IPluginUiSurfaceCapability>(out var capability))
{
    context.Lifetime.Track(capability.Contribute(
        new PluginUiSlotContributionDescriptor(
            "pcl.page.launch",
            "primary-actions.after",
            "dev.example.hello.panel",
            order: 100,
            title: "Hello Panel")));
}
```

声明式 AXAML 仍应通过 Manifest 描述实际视觉内容。Capability 注册也必须跟踪生命周期。

## required 与 fallback

| 配置 | 建议用途 |
|---|---|
| `required: false` + `disable-feature` | 可选面板不可用时，插件其他功能继续 |
| `required: true` + `skip-patch` | 记录失败但继续加载；适合非核心 Patch |
| `required: true` + `fail-load` | UI 是插件存在的唯一目的，缺失时整个插件失败 |

默认优先让功能降级，而不是因为装饰性 UI 变化导致插件完全无法加载。