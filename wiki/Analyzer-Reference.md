# Analyzer 参考

> SDK `0.1.0-alpha.3`

| ID | 含义 |
|---|---|
| PNPSDK001 | 禁止引用 `PCL.Application` |
| PNPSDK002 | 禁止引用 `PCL.Desktop` |
| PNPSDK003 | 禁止引用私有 `PCL.Plugin` |
| PNPSDK004 | 插件入口类型无效 |
| PNPSDK005 | 注册句柄未跟踪 |
| PNPSDK006 | 使用私有宿主命名空间 |
| PNPSDK007 | `plugin.json` 缺少必需字段 |
| PNPSDK008 | 缺少 API 版本范围 |
| PNPSDK009 | AXAML 声明路径无效 |
| PNPSDK010 | 创建未托管后台工作 |

把 `plugin.json` 加入 `AdditionalFiles` 才能启用 Manifest 编译诊断。运行时和打包阶段仍会执行完整验证。
