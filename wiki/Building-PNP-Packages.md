# 构建 `.pnp`

> SDK `0.1.0-alpha.3`

安装 `PCLN.Plugin.Sdk.Build`，设置 `PclNPluginId`，然后执行：

```powershell
dotnet build -c Release
```

构建器会：

1. 规范化 `plugin.json`。
2. 收集 AnyCPU 入口 DLL、私有托管依赖、deps、显式内容和 Manifest 引用的 AXAML。
3. 排除共享/私有宿主程序集。
4. 生成 `META-INF/pnp.files.json`。
5. 生成 `META-INF/pnp.signed.json`。
6. 固定条目顺序和时间戳，创建可复现 ZIP 容器。

开发时可设置 `<PclNPluginSign>false</PclNPluginSign>`，但输出会警告不可正式分发。AXAML 文件默认放在项目的 `ui/` 下，并由 Manifest 自动收集。

## 平台与架构

默认输出是平台无关包：`net10.0` + `AnyCPU` 托管程序集可由 Windows、Linux、macOS 的 x64/arm64 Host 加载。`.NET` 没有名为 `Any Arch` 的目标；对应方案是 `AnyCPU`。

插件包含原生库时，为项目项设置 RID：

```xml
<ItemGroup>
  <PclNPluginNative Include="native/win-x64/example.dll" RuntimeIdentifier="win-x64" />
  <PclNPluginNative Include="native/linux-x64/libexample.so" RuntimeIdentifier="linux-x64" />
  <PclNPluginNative Include="native/osx-arm64/libexample.dylib" RuntimeIdentifier="osx-arm64" />
</ItemGroup>
```

构建器将其放入 `runtimes/<rid>/native/`。一个包可以携带多个 RID 的原生资产；若要发布架构专用包，设置 `<PclNPluginRuntimeIdentifier>win-x64</PclNPluginRuntimeIdentifier>`，输出文件名会添加 `-win-x64` 后缀。未声明 RID 的通用包必须保持 `AnyCPU`。
