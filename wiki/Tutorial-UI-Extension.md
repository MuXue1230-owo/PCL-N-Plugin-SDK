# UI 扩展实战

> 适用于 SDK `0.1.0-alpha.5` 与 PCL.Plugin `v0.11.0`。本教程只使用公开 Capability、Surface 和 Slot。

PCL N 插件有两类常见 UI 接入：

- 在插件设置中注册一个描述页；
- 通过 Manifest 把声明式 AXAML 注入宿主公开的 Slot。

不要反射宿主控件、遍历 Visual Tree、依赖 `x:Name` 或本地化文字。那些都不是兼容契约。

## 1. 注册插件设置页

设置页使用 Capability，而不是假设所有宿主都一定提供：

```csharp
if (context.Capabilities.TryGet<IPluginSettingsPageCapability>(out var pages))
{
    IPluginRegistration page = pages.Register(
        new PluginSettingsPageDescriptor(
            "dev.example.toolbox.settings",
            "Toolbox",
            "lucide/wrench",
            "Toolbox 设置",
            "配置实例扫描与通知行为。",
            [
                new PluginSettingsHintDescriptor(
                    "修改扫描间隔后需要重新启用插件。",
                    PluginSettingsHintKind.Information)
            ])
        {
            Order = 300
        });

    context.Lifetime.Track(page);
}
else
{
    context.Logger.Warn("宿主不支持插件设置页，核心功能继续运行。");
}
```

如果整个插件离开设置页就没有意义，可以使用 `Require<IPluginSettingsPageCapability>()`；大多数插件更适合 `TryGet` 并降级。

## 2. 注册 AXAML 所需命令

声明式 AXAML 不允许代码隐藏。按钮行为通过稳定命令 ID 连接：

```csharp
IPluginCommandService commands =
    context.Services.Require<IPluginCommandService>();

context.Lifetime.Track(commands.Register(
    new PluginCommandDescriptor(
        "dev.example.toolbox.scan-instances",
        "Toolbox：扫描实例",
        ScanAsync,
        description: "立即扫描本地实例。",
        icon: "lucide/search")));
```

Manifest 中的 `command`、AXAML 的 `Commands[...]` 和 C# 注册 ID 必须完全相同。

## 3. 创建声明式 AXAML

新建 `ui/ToolboxPanel.axaml`：

```xml
<Border xmlns="https://github.com/avaloniaui"
        Padding="12"
        CornerRadius="8">
  <StackPanel Spacing="8">
    <TextBlock FontWeight="SemiBold"
               Text="实例工具箱" />
    <TextBlock Text="扫描 PCL N 当前管理的 Minecraft 实例。"
               TextWrapping="Wrap" />
    <Button Content="立即扫描"
            Command="{Binding Commands[dev.example.toolbox.scan-instances]}" />
  </StackPanel>
</Border>
```

安全限制：

- 不写 `x:Class`，不使用代码隐藏；
- 不引用 `PCL.Application`、`PCL.Desktop` 或 `PCL.Plugin` 类型；
- 不从 AXAML 发起任意文件、网络或进程操作；
- 资源路径必须在 Manifest 中声明并被打进 `.pnp`；
- 交互行为只绑定公开命令。

## 4. 在 Manifest 声明目标

把下面内容加入 `plugin.json`：

```json
"ui": {
  "api": {
    "minimum": "0.1",
    "maximumExclusive": "1.0"
  },
  "avalonia": {
    "minimum": "12.0",
    "maximumExclusive": "13.0"
  },
  "targets": [
    {
      "target": "pcl.page.launch",
      "surface": ">=3.0.0 <4.0.0",
      "access": ["inject"],
      "operations": [
        {
          "id": "toolbox-panel",
          "kind": "inject",
          "slot": "primary-actions.after",
          "axaml": "ui/ToolboxPanel.axaml",
          "command": "dev.example.toolbox.scan-instances",
          "priority": 0,
          "required": false,
          "fallback": "disable-feature"
        }
      ]
    }
  ]
}
```

同时加入权限说明：

```json
{
  "id": "ui.inject",
  "reason": "在启动页主操作区后方显示实例扫描按钮。"
}
```

本例使用 `required: false`：若未来启动页 Surface 不兼容，只关闭面板，命令和后台扫描仍可运行。只有 UI 就是插件全部功能时，才考虑 `fail-load`。

## 5. 在代码中预先查询兼容性

运行时会验证 Manifest；代码查询可以提供更清晰的降级日志：

```csharp
IPluginUiSurfaceRegistry ui =
    context.Services.Require<IPluginUiSurfaceRegistry>();

bool panelAvailable = ui.SupportsSlot(
    "pcl.page.launch",
    "primary-actions.after",
    ">=3.0 <4.0",
    PluginUiOperation.Inject);

if (!panelAvailable)
    context.Logger.Warn("启动页面板不可用，保留命令与后台扫描功能。");
```

若在代码中直接使用 `pcl.ui`，Manifest 的 required/optional 服务也要与实际降级策略一致。

## 6. 当前公开 Surface

| Surface | 版本 | 常用 Slot | 适合场景 |
|---|---:|---|---|
| `pcl.window.main` | `1.0` | 无 | 观察主窗口状态 |
| `pcl.navigation.main` | `1.0` | `items.after-download` | 增加导航入口 |
| `pcl.page.launch` | `3.1` | `primary-actions.launch-button`、`primary-actions.after` | 启动按钮附近的工具 |
| `pcl.page.settings` | `1.0` | `sidebar.after-plugin` | 设置导航扩展 |

Surface 独立版本化，不能把 PCL N 产品版本当作 Surface 版本。最新契约以 [UI Surface 与 Slot](UI-Surfaces-and-Slots) 为准。

## 7. 构建与验证

```powershell
dotnet build -c Release
tar -tf .\bin\Release\net10.0\dev.example.toolbox-0.1.0.pnp
```

确认包内存在 `ui/ToolboxPanel.axaml`。桌面端验证时覆盖这些情况：

- 面板出现在正确 Slot，窗口缩放时没有越界；
- 按钮只触发一次命令；
- 插件停用后面板和命令都消失；
- Surface 不可用时插件仍能启用；
- 浅色、深色主题和 Linux 字体环境下文本仍可读；
- 不同窗口尺寸下不使用固定宽高挤压宿主布局。

## 常见问题

| 现象 | 检查项 |
|---|---|
| AXAML 未进入包 | 路径是否与 Manifest 完全一致，文件是否位于内容根目录 |
| 按钮点击无反应 | 三处命令 ID 是否一致，命令是否已注册且被 Lifetime 跟踪 |
| 更新宿主后面板消失 | Surface 版本范围是否仍匹配；查看兼容日志 |
| 插件因装饰性 UI 无法加载 | 把操作改为 `required: false` 并提供 `disable-feature` |
| 布局在小窗口越界 | 移除固定尺寸，使用 `TextWrapping`、合理 Padding 和宿主 Slot 布局 |

更复杂的修改、替换、包裹和冲突排序见 [UI Patch 与冲突](UI-Patches-and-Conflicts)。
