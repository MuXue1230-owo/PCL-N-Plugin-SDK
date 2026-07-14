# OpenPGP 签名

> SDK `0.1.0-alpha.4`

推荐 Ed25519 主密钥和签名子密钥，也支持 RSA 3072+。始终使用完整指纹。

```xml
<PropertyGroup>
  <PclNPluginSign>true</PclNPluginSign>
  <PclNPluginGpgPath>gpg</PclNPluginGpgPath>
</PropertyGroup>
```

指纹来自 `plugin.json` 的 `signing.fingerprint`。构建器对 `META-INF/pnp.signed.json` 创建 armored detached signature，并导出公钥到 `META-INF/keys/`。私钥不得写入仓库、NuGet 包或 `.pnp`。
