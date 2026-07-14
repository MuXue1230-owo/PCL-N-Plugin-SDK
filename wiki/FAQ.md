# FAQ

> SDK `0.1.0-alpha.2`

## 为什么 `PCL.Plugin` 不公开？

它是 PCL-N 内置特权 Host，包含加载、安全、市场和恢复实现。第三方只依赖公开 ABI，可避免 Host 内部重构破坏插件。

## 能否直接写 AXAML？

可以。Manifest 的 `axaml` 字段引用 `ui/*.axaml`。首版仅支持无 `x:Class` 的声明式 AXAML，行为通过命令 ID 和公开服务绑定。

## 权限是否等于沙箱？

不是。插件在进程内运行，权限只能约束官方 API。

## 可以引用 Avalonia 吗？

声明式 AXAML由 Host 加载。Raw Avalonia 能力属于高风险可选能力，必须由 Host 支持并获得对应权限。
