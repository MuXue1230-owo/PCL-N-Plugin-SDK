# NuGet 包

> SDK `0.1.0-alpha.3`

| 包 | 用途 |
|---|---|
| `PCLN.Plugin.Abstractions` | 入口、生命周期、服务和 UI 公共 ABI |
| `PCLN.Plugin.Sdk` | Manifest 验证、版本范围和辅助扩展 |
| `PCLN.Plugin.Analyzers` | `PNPSDK001–010` 编译诊断 |
| `PCLN.Plugin.Testing` | 不启动 PCL-N 的内存测试宿主 |
| `PCLN.Plugin.Sdk.Build` | `.pnp` 打包、文件表、可复现构建和 GPG 签名 |

NuGet 包 ID 使用 `PCLN.Plugin.*`；程序集名与命名空间仍使用 `PCL.N.Plugin.*`，现有源码中的 `using` 不需要修改。

普通插件项目建议引用前四个开发包中的 Abstractions、Sdk、Analyzers、Sdk.Build；Testing 只应进入测试项目。
