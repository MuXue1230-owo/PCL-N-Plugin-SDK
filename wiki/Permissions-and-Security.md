# 权限与安全

> SDK `0.1.0-alpha.2`

权限声明用于描述和约束官方 API，不是操作系统沙箱。进程内插件仍可能调用 .NET 和系统 API，用户只应安装可信签名来源。

正式 `.pnp` 必须包含有效 OpenPGP 签名。高风险权限包括全局 UI、Raw UI、输入拦截、页面替换、账户、启动和原生代码。

AXAML 安全规则：

- 禁止 `x:Class` 和代码隐藏。
- 禁止 `PCL.Application`、`PCL.Desktop`、`PCL.Plugin` CLR 命名空间。
- 只允许 Manifest 提前声明的包内路径。
- 事件通过公开命令和服务绑定。
- Host 应限制可实例化类型、MarkupExtension 和资源 URI。
