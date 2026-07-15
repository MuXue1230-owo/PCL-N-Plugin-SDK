# 依赖与打包实战

> 适用于 SDK `0.1.0`。本页区分插件前置、NuGet 托管依赖、内容文件和原生资产。

这四类依赖的安装和加载方式不同，不能混为一谈：

| 类型 | 示例 | 应放在哪里 |
|---|---|---|
| 插件前置 | `dev.example.core` | Manifest 的 `dependencies`，不嵌入当前 `.pnp` |
| 宿主服务 | `pcl.commands` | Manifest 的 `services`，由 PCL N 提供 |
| 托管库 | `YamlDotNet.dll` | 普通 NuGet/项目引用，构建器自动收集私有 DLL |
| 内容/原生资产 | JSON 模板、`.dll`、`.so` | `PclNPluginContent` 或 `PclNPluginNative` |

## 1. 声明插件前置

```json
"dependencies": [
  {
    "id": "dev.example.core",
    "version": ">=1.2.0 <2.0.0",
    "kind": "required"
  },
  {
    "id": "dev.example.integration",
    "version": ">=0.5.0 <1.0.0",
    "kind": "optional"
  }
]
```

- required 前置缺失、禁用或版本不匹配时，当前插件不会加载。
- optional 前置缺失时必须保留可用的降级路径。
- required 范围应有 Major 上界，不要假定未来破坏性版本一定兼容。
- 依赖图不能成环；共同逻辑应拆成一个方向明确的基础插件或普通库。

`.pnp` 不会把另一个插件嵌入包中。直接安装本地 `.pnp` 时，应先安装并启用 required 前置；从插件中心安装时，详情页显示和自动处理前置依赖取决于市场元数据与桌面端版本，Manifest 仍是运行时最终校验依据。

## 2. 声明不兼容插件

```json
"incompatibilities": [
  {
    "id": "dev.example.legacy-toolbox",
    "version": "<2.0.0",
    "reason": "两者会占用同一启动页功能；请升级旧插件到 2.0.0 以上。"
  }
]
```

`reason` 要告诉用户后果和解决办法。运行时会双向检查冲突，所以不能依赖安装顺序绕过声明。

## 3. 引用普通托管库

例如插件需要 JSON 之外的 YAML 解析器：

```xml
<ItemGroup>
  <PackageReference Include="YamlDotNet" Version="16.3.0" />
</ItemGroup>
```

构建器从 `ReferenceCopyLocalPaths` 收集运行时私有程序集，并放入 `.pnp`。无需手工复制 DLL。构建后检查：

```powershell
dotnet build -c Release
tar -tf .\bin\Release\net10.0\dev.example.toolbox-0.1.0.pnp
```

宿主共享或私有程序集会被排除：

- `PCL.N.Plugin.Abstractions.dll`
- `PCL.Application.dll`
- `PCL.Desktop.dll`
- `PCL.Plugin.dll`

如果业务库依赖了这些宿主私有程序集，应修改业务库边界，而不是尝试强制把 DLL 塞进包内。

## 4. 添加配置模板和静态内容

项目文件：

```xml
<ItemGroup>
  <PclNPluginContent Include="assets/default-rules.json" />
  <PclNPluginContent Include="README.md" />
</ItemGroup>
```

代码读取时，以插件包或数据目录的公开路径约定为准；需要修改的默认配置应在首次运行时复制到 `context.Directories.Data`，不要尝试修改包内文件。

Manifest 引用的 `ui/*.axaml` 会由构建器自动收集。路径必须是内容根目录内的安全相对路径，不能包含 `..` 逃逸。

## 5. 携带原生库

```xml
<ItemGroup>
  <PclNPluginNative Include="native/win-x64/example.dll"
                    RuntimeIdentifier="win-x64" />
  <PclNPluginNative Include="native/linux-x64/libexample.so"
                    RuntimeIdentifier="linux-x64" />
  <PclNPluginNative Include="native/osx-arm64/libexample.dylib"
                    RuntimeIdentifier="osx-arm64" />
</ItemGroup>
```

包内路径为 `runtimes/<rid>/native/`。发布前至少在每个声明的平台和架构上实际加载一次；仅仅看到文件进入包不代表 ABI、系统库和文件权限都正确。

若只发布单架构包：

```xml
<PclNPluginRuntimeIdentifier>win-x64</PclNPluginRuntimeIdentifier>
```

输出文件名会带 `-win-x64`。纯托管插件保持 AnyCPU，避免无意义地拆分平台包。

## 6. 检查 `.pnp` 结构

典型结构：

```text
plugin.json
lib/net10.0/PclNToolbox.Plugin.dll
lib/net10.0/YamlDotNet.dll
ui/ToolboxPanel.axaml
assets/default-rules.json
runtimes/win-x64/native/example.dll
META-INF/pnp.files.json
META-INF/pnp.signed.json
META-INF/signatures/<fingerprint>.asc
META-INF/keys/<fingerprint>.asc
```

逐项检查：

- Manifest 的入口 DLL 和类型存在；
- 所有 AXAML 和内容路径大小写一致；
- 第三方许可证允许重新分发；
- 没有测试程序集、Debug 符号、密钥、Token 或本机路径配置；
- 没有宿主私有程序集；
- `META-INF/pnp.files.json` 覆盖所有受保护文件。

## 7. 可复现构建

在同一提交、SDK 和锁定依赖上构建两次：

```powershell
dotnet clean -c Release
dotnet build -c Release
Get-FileHash .\bin\Release\net10.0\dev.example.toolbox-0.1.0.pnp -Algorithm SHA256
```

重复后哈希应相同。若不同，检查：

- 构建过程中是否动态生成当前时间或随机 ID；
- NuGet 依赖是否未锁定而发生漂移；
- 输入文件是否包含机器相关绝对路径；
- 签名工具或密钥输出是否含不稳定元数据。

## 8. 版本和签名

以下值在发布时必须一致：

```text
项目 <Version>
  = plugin.json version
  = Git Tag 去掉 v 后的版本
  = 插件中心发布版本
```

正式包设置：

```xml
<PclNPluginSign>true</PclNPluginSign>
```

并把 Manifest 的 `signing.fingerprint` 改为正式 OpenPGP 公钥完整指纹。密钥准备和 CI 导入方式见 [OpenPGP 签名](OpenPGP-Signing)。

下一步使用 [测试、调试与发布实战](Tutorial-Test-Debug-Release) 建立完整质量门槛。
