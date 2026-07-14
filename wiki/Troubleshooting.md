# 故障排查

> SDK `0.1.0-alpha.4`

## 没有生成 `.pnp`

确认已引用 `PCLN.Plugin.Sdk.Build`、设置 `PclNPluginId`，并存在 `plugin.json`。

## AXAML 未进入包

确认 Manifest 使用 `ui/...axaml` 安全相对路径，文件实际位于项目对应位置。禁止 `x:Class` 和私有宿主 CLR 命名空间。

## GPG 失败

确认 `gpg` 可执行、私钥存在、使用完整指纹，并且签名子密钥未过期或吊销。

## PNPSDK005

保存注册结果并调用 `context.Lifetime.Track(registration)`。

## 插件加载失败

检查 Manifest API/Host 范围、服务、权限、签名和入口类型。不要把 `PCL.N.Plugin.Abstractions.dll` 放进 `.pnp`。
