# 故障排查

> SDK `0.1.0`

## NuGet 找不到包

确认包 ID 使用 `PCLN.Plugin.*`，不是旧的 `PCL.N.Plugin.*`：

```powershell
dotnet nuget list source
dotnet add package PCLN.Plugin.Abstractions --version 0.1.0 --source https://api.nuget.org/v3/index.json
```

清理失败缓存：

```powershell
dotnet nuget locals all --clear
dotnet restore
```

## 没有生成 `.pnp`

检查：

- 已引用 `PCLN.Plugin.Sdk.Build`；
- `<PclNPluginBuildEnabled>true</PclNPluginBuildEnabled>`；
- 已设置 `PclNPluginId`；
- `plugin.json` 存在于 `PclNPluginManifest` 指向的位置；
- 运行的是真正 Build，不是仅设计时构建。

## PNPBUILD005

缺少 `PclNPluginId`：

```xml
<PclNPluginId>dev.example.plugin</PclNPluginId>
```

它必须与 Manifest `id` 一致。

## PNPBUILD006

通用包不是 AnyCPU。纯托管插件使用：

```xml
<PlatformTarget>AnyCPU</PlatformTarget>
```

架构专用包则设置 `PclNPluginRuntimeIdentifier`。

## AXAML 未进入包

- Manifest 使用 `ui/...axaml`；
- 路径只用 `/`，不含 `..` 或反斜杠；
- 文件实际存在于 `PclNPluginContentRoot` 下；
- 不含 `x:Class` 或私有宿主 CLR 命名空间。

## GPG 签名失败

```powershell
gpg --version
gpg --list-secret-keys --with-subkey-fingerprint
```

确认：

- `gpg` 可执行；
- Manifest 使用完整主密钥指纹；
- 私钥/签名子密钥存在且未过期、未吊销；
- CI 已导入私钥并能非交互使用；
- 没有把短 Key ID 当作指纹。

## 插件安装失败

按顺序检查：

1. ZIP 路径和文件表是否安全；
2. 每个文件 SHA-256 是否匹配；
3. OpenPGP 签名与 Manifest 指纹是否匹配；
4. `entryPoint.assembly` 是否存在；
5. API/Host/服务版本范围是否兼容；
6. required 前置是否安装且启用；
7. 是否误带共享或私有宿主程序集。

## 插件显示“缺少前置”

打开“设置 → 插件 → 已安装”查看缺少的 ID 和版本范围。先安装并启用 required 前置，再启用当前插件。检查依赖图是否出现环。

## 插件能安装但无法启用

- required 服务不可用；
- Manifest 不兼容另一已启用插件；
- Plugin Safe Mode 已开启；
- 包未签名、开发签名损坏或签名指纹与公钥不一致；
- `InitializeAsync` 抛出异常；
- 入口类型不符合要求。

## UI 没有出现

- Surface ID、版本范围或 Slot 不匹配；
- `access` 和权限缺失；
- UI Safe Mode 跳过高风险 Patch；
- AXAML 安全验证失败；
- Patch 与其他插件冲突；
- `fallback` 让可选功能被禁用。

在开发者模式中显示“UI Patch”和“兼容性”页面查看计划与冲突。

## PNPSDK005

保存注册结果并调用：

```csharp
context.Lifetime.Track(registration);
```

## 插件停用后仍占用文件

通常表示插件保留了静态引用、事件订阅、后台线程或未释放资源：

- 避免静态保存宿主服务或插件实例；
- 所有注册交给 Lifetime；
- 后台工作使用 `IPluginTaskService`；
- 取消后等待任务结束；
- 释放流、Watcher、Timer 和原生句柄。
