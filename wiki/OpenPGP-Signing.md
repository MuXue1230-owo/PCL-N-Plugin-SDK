# OpenPGP 签名

> SDK `0.1.0-alpha.4`。正式分发的 `.pnp` 必须签名。

PCL N 使用 OpenPGP detached signature 验证发布者身份和包内容。推荐 Ed25519 主密钥与签名子密钥，也支持 RSA 3072 位及以上。SHA-1、过期或吊销密钥会被拒绝。

## 1. 安装 GnuPG

```powershell
gpg --version
```

使用 GnuPG 2.x。Windows 可安装 Gpg4win；Linux/macOS 使用系统包管理器。

## 2. 创建密钥

交互方式：

```powershell
gpg --full-generate-key
```

为插件发布使用独立密钥，设置强密码并妥善备份撤销证书。发布者团队应明确谁可以使用签名子密钥。

## 3. 获取完整指纹

```powershell
gpg --list-secret-keys --with-subkey-fingerprint --keyid-format long
```

把主密钥完整指纹写进 Manifest：

```json
"signing": {
  "fingerprint": "0123456789ABCDEF0123456789ABCDEF01234567"
}
```

不要使用 8/16 位短 Key ID，也不要写空格。

## 4. 启用构建签名

```xml
<PropertyGroup>
  <PclNPluginSign>true</PclNPluginSign>
  <PclNPluginGpgPath>gpg</PclNPluginGpgPath>
</PropertyGroup>
```

```powershell
dotnet build -c Release
```

构建器会：

- 对 `META-INF/pnp.signed.json` 创建 armored detached signature；
- 写入 `META-INF/signatures/<fingerprint>.asc`；
- 导出公钥到 `META-INF/keys/<fingerprint>.asc`；
- 让文件表和 payload root 覆盖包内容。

## 5. 检查包内容

`.pnp` 是 ZIP，可复制到临时目录后解压查看：

```powershell
Copy-Item .\bin\Release\net10.0\dev.example.plugin-1.0.0.pnp plugin.zip
Expand-Archive .\plugin.zip .\plugin-inspect
Get-ChildItem .\plugin-inspect\META-INF -Recurse
```

不要修改并重新压缩正式包；任何内容变化都会破坏哈希或签名。

## CI 中签名

将私钥保存在 CI 的加密 Secret 中，运行时导入临时 GPG home。不要把私钥、密码、导出文件或 Secret 输出到日志。

```yaml
- name: Import signing key
  shell: bash
  env:
    PNP_GPG_PRIVATE_KEY: ${{ secrets.PNP_GPG_PRIVATE_KEY }}
  run: |
    set -euo pipefail
    printf '%s' "$PNP_GPG_PRIVATE_KEY" | gpg --batch --import

- name: Build signed package
  run: dotnet build -c Release -p:PclNPluginSign=true
```

若密钥有密码，使用支持非交互 pinentry 的安全方案，并把密码放在独立 Secret。不要使用 `--passphrase` 把明文暴露在进程列表或日志中。

## 密钥轮换

1. 用旧密钥发布说明新指纹的版本或公告。
2. 在插件中心完成发布者密钥验证。
3. 新版本 Manifest 改用新完整指纹。
4. 保留旧公钥用于验证历史版本。
5. 密钥泄露时立即吊销，并停止分发所有受影响版本。
