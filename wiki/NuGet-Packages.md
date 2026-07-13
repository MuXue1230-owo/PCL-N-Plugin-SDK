# NuGet 包

> SDK `0.1.0-alpha.1`

| 包 | 用途 |
|---|---|
| `PCL.N.Plugin.Abstractions` | 入口、生命周期、服务和 UI 公共 ABI |
| `PCL.N.Plugin.Sdk` | Manifest 验证、版本范围和辅助扩展 |
| `PCL.N.Plugin.Analyzers` | `PNPSDK001–010` 编译诊断 |
| `PCL.N.Plugin.Testing` | 不启动 PCL-N 的内存测试宿主 |
| `PCL.N.Plugin.Sdk.Build` | `.pnp` 打包、文件表、可复现构建和 GPG 签名 |

普通插件项目建议引用前四个开发包中的 Abstractions、Sdk、Analyzers、Sdk.Build；Testing 只应进入测试项目。
