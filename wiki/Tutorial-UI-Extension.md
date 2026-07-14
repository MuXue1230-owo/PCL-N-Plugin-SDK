# UI 扩展实战

> 适用于 SDK `0.1.0-alpha.5` 与 PCL.Plugin `v0.11.0`。本教程只使用公开 Capability、Surface 和 Slot。

PCL N 插件可按风险与耦合程度选择五类 UI 接入。优先使用能满足需求的最低层能力：

| 接入方式 | 主要契约 | 适合场景 | 额外包 |
|---|---|---|---|
| 设置描述页 | `IPluginSettingsPageCapability` | 展示说明、提示和宿主管理的设置入口 | `PCLN.Plugin.Abstractions` |
| 声明式 Slot 注入 | Manifest `ui.targets` + AXAML | 在宿主公开位置加入轻量内容 | `PCLN.Plugin.Abstractions` |
| 完整主导航页面 | `IAvaloniaPluginPageService` | 插件拥有完整页面布局和交互 | `PCLN.Plugin.UI.Avalonia` |
| 插件窗口 | `IAvaloniaPluginWindowService` | 独立工具、编辑器或诊断窗口 | `PCLN.Plugin.UI.Avalonia` |
| Raw Avalonia | `IAvaloniaUiAccessService` | 经审计访问真实 Avalonia对象 | `PCLN.Plugin.UI.Avalonia` |

`PCLN.Plugin.UI` 提供 `UiTargetId`、`PluginPageDescriptor`、`IPluginNavigationService` 和服务 ID，不引入具体 UI框架。只有需要 `Control`、`Window`、完整页面/窗口工厂或 Raw Avalonia时，才引用 `PCLN.Plugin.UI.Avalonia`。

公开契约不代表每个 Host版本都提供对应服务。完整页面、窗口和 Raw Avalonia默认应声明为可选服务，并使用 `TryGet`降级。不要反射宿主控件、遍历 Visual Tree、依赖 `x:Name` 或本地化文字；它们都不是兼容契约。

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

## 6. 声明可选 UI 服务与权限

完整页面、窗口和 Raw Avalonia是独立服务。除非插件明确要求只运行在已提供这些服务的 Host版本上，否则先放入 `services.optional`：

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0"
  },
  "optional": {
    "pcl.navigation": ">=0.1 <1.0",
    "pcl.ui.avalonia": ">=0.1 <1.0",
    "pcl.ui.avalonia.pages": ">=0.1 <1.0",
    "pcl.ui.avalonia.windows": ">=0.1 <1.0"
  }
},
"permissions": [
  {
    "id": "ui.register",
    "reason": "在主导航中注册插件拥有的工具页面。"
  },
  {
    "id": "ui.window-management",
    "reason": "按用户操作打开和管理插件工具窗口。"
  },
  {
    "id": "ui.raw-access",
    "reason": "读取宿主公开 UI Target 的尺寸以调整插件内容。"
  }
],
"ui": {
  "api": { "minimum": "0.1", "maximumExclusive": "1.0" },
  "avalonia": { "minimum": "12.0", "maximumExclusive": "13.0" },
  "targets": [
    {
      "target": "pcl.navigation.main",
      "surface": ">=1.0 <2.0",
      "access": ["register"],
      "operations": [
        {
          "id": "dev.example.toolbox.page-registration",
          "kind": "register",
          "required": false,
          "fallback": "disable-feature"
        }
      ]
    },
    {
      "target": "pcl.window.main",
      "surface": ">=1.0 <2.0",
      "access": ["window-management"],
      "operations": [
        {
          "id": "dev.example.toolbox.window-registration",
          "kind": "register",
          "required": false,
          "fallback": "disable-feature"
        }
      ]
    }
  ]
}
```

只声明实际使用的服务和权限。若插件没有 Raw访问代码，就删除 `pcl.ui.avalonia` 和 `ui.raw-access`；若没有独立窗口，也删除窗口服务和权限。

## 7. 注册完整主导航页面

完整页面需要 `PCLN.Plugin.UI.Avalonia`。页面由插件创建，但由 Host注册到主导航并管理入口：

```csharp
using Avalonia.Controls;
using PCL.N.Plugin;

if (context.Services.TryGet<IAvaloniaPluginPageService>(out var pages))
{
    var descriptor = new AvaloniaPluginPageDescriptor(
        new PluginPageDescriptor(
            "dev.example.toolbox.page-registration",
            "dev.example.toolbox.page",
            "实例工具箱",
            icon: "lucide/wrench",
            order: 500),
        static () => new StackPanel
        {
            Spacing = 12,
            Children =
            {
                new TextBlock { Text = "实例工具箱" },
                new TextBlock { Text = "这里是插件拥有的完整页面。" }
            }
        });

    context.Lifetime.Track(pages.Register(descriptor));

    await pages.NavigateAsync(
        "dev.example.toolbox.page",
        cancellationToken);
}
else
{
    context.Logger.Warn("当前 Host 不提供完整插件页面，保留命令行入口。");
}
```

规则：

- `operationId` 必须全局稳定，推荐以插件 ID开头；
- `route` 同样应位于插件自己的命名空间，不要冒充宿主路由；
- `createPage` 每次由 Host调用时都应返回新的 `Control`，不要复用已挂载实例；
- 必须把 `Register` 返回值交给 `context.Lifetime.Track`，插件停用后导航入口才会释放；
- `NavigateAsync` 只接受已注册或 Host公开的稳定路由，不要猜测内部页面名称。

## 8. 注册和显示插件窗口

独立工具窗口使用 `IAvaloniaPluginWindowService`：

```csharp
using Avalonia;
using Avalonia.Controls;
using PCL.N.Plugin;

if (context.Services.TryGet<IAvaloniaPluginWindowService>(out var windows))
{
    context.Lifetime.Track(windows.Register(
        new AvaloniaPluginWindowDescriptor(
            "dev.example.toolbox.window-registration",
            "dev.example.toolbox.inspector",
            static () => new Window
            {
                Title = "Toolbox Inspector",
                Width = 720,
                Height = 480,
                Content = new TextBlock
                {
                    Margin = new Thickness(20),
                    Text = "插件诊断信息"
                }
            })));

    await windows.ShowAsync(
        "dev.example.toolbox.inspector",
        cancellationToken);

    context.Logger.Info($"窗口已打开；当前插件窗口数：{windows.ListOpenWindows().Count}");
}
else
{
    context.Logger.Warn("当前 Host 不支持插件窗口，改用通知或完整页面。 ");
}
```

`Register` 注册窗口工厂，`ShowAsync` 才创建或显示窗口。不要自行把窗口塞入宿主内部集合；不要缓存已关闭窗口；插件停用时注册会随生命周期释放，窗口的关闭或回收策略由 Host实现决定。

## 9. 谨慎使用 Raw Avalonia

Raw访问用于无法通过页面、窗口、Slot或公开命令完成的受审计场景：

```csharp
using Avalonia.Controls;
using PCL.N.Plugin;

if (context.Services.TryGet<IAvaloniaUiAccessService>(out var avalonia))
{
    var target = new UiTargetId("pcl.page.launch");
    Control? control = await avalonia.ResolveControlAsync(target, cancellationToken);

    if (control is not null)
    {
        string targetType = await avalonia.InvokeAsync(
            ui => ui.ResolveControl(target)?.GetType().Name ?? "unavailable",
            cancellationToken);
        context.Logger.Debug($"公开 Target 类型：{targetType}");
    }
}
```

安全和兼容边界：

- 只解析 Host明确公开的 `UiTargetId`，不要遍历 Visual Tree寻找私有控件；
- 不要长期缓存 `Control`、`TopLevel` 或 `Application`。Host重建页面后旧对象可能失效；
- 需要跨重建访问时优先使用 Host提供的 generation-aware `IUiTargetHandle`，或每次重新解析 Target；
- 所有 Avalonia对象访问都应在服务提供的 UI上下文中完成；
- Raw访问不能绕过 Manifest权限、Surface版本、Safe Mode或用户审核；
- 能用完整页面、插件窗口、声明式 Slot或公开命令完成时，不申请 `ui.raw-access`。

## 10. 当前公开 Surface

| Surface | 版本 | 常用 Slot | 适合场景 |
|---|---:|---|---|
| `pcl.window.main` | `1.0` | 无 | 观察主窗口状态 |
| `pcl.navigation.main` | `1.0` | `items.after-download` | 增加导航入口 |
| `pcl.page.launch` | `3.1` | `primary-actions.launch-button`、`primary-actions.after` | 启动按钮附近的工具 |
| `pcl.page.settings` | `1.0` | `sidebar.after-plugin` | 设置导航扩展 |

Surface 独立版本化，不能把 PCL N 产品版本当作 Surface 版本。最新契约以 [UI Surface 与 Slot](UI-Surfaces-and-Slots) 为准。

## 11. 构建与验证

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
