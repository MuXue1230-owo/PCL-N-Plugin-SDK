# 构建 `.pnp`

> SDK `0.1.0-alpha.5`

安装 `PCLN.Plugin.Sdk.Build` 后，MSBuild 会在普通项目构建结束时生成 `.pnp`。

## 最小配置

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <Version>1.2.3</Version>
  <PclNPluginId>dev.example.plugin</PclNPluginId>
  <PclNPluginSign>false</PclNPluginSign>
</PropertyGroup>

<ItemGroup>
  <PackageReference Include="PCLN.Plugin.Sdk.Build"
                    Version="0.1.0-alpha.5"
                    PrivateAssets="all" />
  <AdditionalFiles Include="plugin.json" />
</ItemGroup>
```

```powershell
dotnet build -c Release
```

默认输出：

```text
bin/Release/net10.0/dev.example.plugin-1.2.3.pnp
```

## MSBuild 属性

| 属性 | 默认值 | 说明 |
|---|---|---|
| `PclNPluginBuildEnabled` | `true` | 是否随 Build 生成 `.pnp` |
| `PclNPluginId` | 无 | 必填；输出名与 Manifest ID |
| `PclNPluginManifest` | `$(MSBuildProjectDirectory)/plugin.json` | Manifest 路径 |
| `PclNPluginContentRoot` | 项目目录 | 内容安全相对路径根目录 |
| `PclNPluginOutputDirectory` | `$(TargetDir)` | 输出目录 |
| `PclNPluginSign` | `false` | `true` 使用发布者密钥；`false` 自动使用本机开发密钥；两者都会签名 |
| `PclNPluginGpgPath` | `gpg` | GPG 可执行文件 |
| `PclNPluginRuntimeIdentifier` | 空 | 架构专用包的 RID |

## 构建器会做什么

1. 解析并验证 `plugin.json`。
2. 规范化 JSON。
3. 收集 AnyCPU 入口 DLL、`.deps.json`、私有托管依赖和显式内容。
4. 收集 Manifest 引用的 `ui/*.axaml`。
5. 排除 `PCL.N.Plugin.Abstractions.dll` 和启动器私有程序集。
6. 生成 `META-INF/pnp.files.json`。
7. 生成 `META-INF/pnp.signed.json`。
8. 可选创建 detached OpenPGP 签名和导出公钥。
9. 固定 ZIP 条目顺序与时间戳，生成可复现容器。

## 添加普通内容

```xml
<ItemGroup>
  <PclNPluginContent Include="assets/default-config.json" />
  <PclNPluginContent Include="README.md" />
</ItemGroup>
```

文件必须位于 `PclNPluginContentRoot` 下，包内使用安全相对路径。

## 托管依赖

NuGet/项目引用中需要随插件运行的私有程序集会从 `ReferenceCopyLocalPaths` 自动收集。以下共享/私有宿主程序集会被排除：

- `PCL.N.Plugin.Abstractions.dll`
- `PCL.Application.dll`
- `PCL.Desktop.dll`
- `PCL.Plugin.dll`

插件不得通过复制 DLL 绕过该边界。

## 平台和原生资产

纯托管插件使用 `AnyCPU`，同一 `.pnp` 可被 Windows、Linux、macOS 的 x64/arm64 Host 加载。

携带原生库：

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

构建器放入 `runtimes/<rid>/native/`。一个包可以携带多个 RID。

只发布单架构包：

```xml
<PclNPluginRuntimeIdentifier>win-x64</PclNPluginRuntimeIdentifier>
```

输出名会增加 `-win-x64`。通用包若不是 AnyCPU 会触发 PNPBUILD006。

## 自定义输出目录

```xml
<PclNPluginOutputDirectory>$(MSBuildProjectDirectory)/artifacts/</PclNPluginOutputDirectory>
```

请保留末尾目录分隔符，CI 可统一上传 `artifacts/*.pnp`。

## 验证可复现性

在相同源码、SDK 和依赖下构建两次：

```powershell
dotnet build -c Release
Get-FileHash .\bin\Release\net10.0\dev.example.plugin-1.2.3.pnp -Algorithm SHA256
```

哈希应一致。版本、编译器、依赖或签名内容变化会合理地改变哈希。
