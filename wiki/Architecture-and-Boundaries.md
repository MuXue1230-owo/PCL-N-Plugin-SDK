# 架构与边界

> SDK `0.1.0-alpha.1`

```text
PCL-N（启动器核心）
  -> PCL.Plugin（私有、特权插件平台）
  -> PCL.N.Plugin.Abstractions（公开 ABI）
  -> 第三方插件
```

第三方插件只引用公开 SDK。`PCL.Plugin` 私有并不影响插件开发：它实现公开服务、加载 `.pnp`、验证签名并管理生命周期。

禁止引用：

- `PCL.Application`
- `PCL.Desktop`
- `PCL.Plugin`
- 启动器内部服务容器

插件在进程内运行。权限声明、签名和审核用于降低风险，但不构成操作系统级沙箱。参见 [[Permissions-and-Security]]。
