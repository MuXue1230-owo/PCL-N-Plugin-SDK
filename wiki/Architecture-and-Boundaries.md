# 架构与边界

> Applies to PCL N Plugin SDK 0.2.0.

> SDK `0.2.0`

```text
PCL N 桌面应用
  └─ PCL.Plugin（私有、特权 Host）
      ├─ 验证并安装 .pnp
      ├─ 提供稳定服务与 UI Surface
      ├─ 管理依赖、生命周期、安全模式和卸载
      └─ 共享 PCL.N.Plugin.Abstractions ABI
          └─ 第三方插件（独立可卸载 AssemblyLoadContext）
```

## 三层职责

| 层 | 职责 | 第三方能否引用 |
|---|---|---:|
| PCL N | 启动器核心、桌面 UI、Minecraft 功能 | 否 |
| PCL.Plugin | 插件加载、安全、市场、恢复和宿主桥接 | 否 |
| PCLN.Plugin SDK | 公共 ABI、开发工具、Analyzer、测试与打包 | 是 |

`PCL.Plugin` 私有不影响开发：它实现公开接口，但内部类型、目录结构和服务容器可以自由重构。插件只面对稳定契约。

## 禁止的依赖

- `PCL.Application`
- `PCL.Desktop`
- `PCL.Plugin`
- 启动器内部 DI/服务容器
- 私有 CLR 命名空间或反射访问

对应 Analyzer PNPSDK001–003 和 PNPSDK006 会在编译期提示。

## 允许的交互方式

- `IPluginContext` 与稳定 `IPluginService`；
- 公开 DTO，例如 `PluginInstanceInfo`；
- Capability，例如设置页注册；
- Manifest 声明的服务、权限、依赖和 UI；
- 宿主发布的 Surface/Slot；
- `context.Directories` 提供的插件私有目录。

## 加载隔离

每个插件使用可回收的 AssemblyLoadContext：

- Abstractions 从默认上下文共享，保证 ABI 类型身份一致；
- 插件私有托管依赖从自身包解析；
- 原生依赖从 `runtimes/<rid>/native/` 探测；
- 停用时释放注册、清理引用并尝试卸载。

隔离加载上下文不是安全沙箱。进程内插件仍能调用 .NET 和操作系统 API，因此签名、权限、审核和用户信任都不可省略。

## 为什么使用声明式 UI

直接操作 Avalonia Visual Tree 会把插件绑定到控件实现。Surface/Slot 把契约缩小为稳定 ID 和版本范围，AXAML 只描述视觉内容，行为通过命令连接。这样宿主可以验证权限、解决冲突、应用安全模式并在卸载时撤销贡献。