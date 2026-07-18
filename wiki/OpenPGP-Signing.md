# OpenPGP 签名

> SDK `0.2.1`。正式分发的 `.pnp` 必须签名。

PCL N 使用 OpenPGP detached signature 验证发布者身份和包内容。推荐 Ed25519 主密钥与签名子密钥，也支持 RSA 3072 位及以上。SHA-1、过期或吊销密钥会被拒绝。

开发构建同样不会生成未签名包。`PclNPluginSign=false` 表示由 SDK 创建并使用仅限本机的开发密钥；该包可以手动安装测试，但不能作为插件中心候选包。

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

把主密钥完整指纹写进 Manifest。单签插件只需要 `fingerprint`：

```json
"signing": {
  "fingerprint": "0123456789ABCDEF0123456789ABCDEF01234567"
}
```

团队多签或轮换期间可以同时声明多个 active key，并用 `minimumValidSignatures` 要求至少多少个签名通过：

```json
"signing": {
  "fingerprint": "0123456789ABCDEF0123456789ABCDEF01234567",
  "fingerprints": [
    "89ABCDEF0123456789ABCDEF0123456789ABCDEF"
  ],
  "revokedFingerprints": []
},
"signingPolicy": {
  "minimumValidSignatures": 2,
  "roles": ["maintainer", "release"]
}
```

不要使用 8/16 位短 Key ID，也不要写空格。`revokedFingerprints` 中的 key 会被运行时和插件中心拒绝，即使包内仍带有对应签名。

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
- 为 Manifest 中每个 active signing fingerprint 写入 `META-INF/signatures/<fingerprint>.asc`；
- 导出对应公钥到 `META-INF/keys/<fingerprint>.asc`；
- 在 signed payload 中写入 `signingKeyFingerprints`；
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

## 插件中心的第二层网站签名

上传时，你的 `.pnp` 是“开发者自签名候选包”。插件中心会重新完成文件表、逐文件哈希、payload root、公钥完整指纹和 detached signature 验证。审核通过后，候选包不会直接分发；网站会保留原始负载与开发者签名，并追加：

```text
META-INF/market/pcln-market-v1.json
META-INF/market/signatures/<market-fingerprint>.asc
META-INF/market/keys/<market-fingerprint>.asc
```

网站签名封套绑定候选包 SHA-256、payload root、插件 ID/版本、开发者指纹、审核记录与时间。网站私钥只保存在服务端 Secret/HSM，不会下发到开发者后台或浏览器。

从插件中心安装时，PCL 会先验证包内两层 OpenPGP 签名，再将最终分发包 SHA-256、插件 ID/版本和两个指纹发送到 `POST /v1/packages/verify`。接口必须明确返回当前状态为 `published`；包已吊销、记录不匹配、网络失败或服务不可用都会拒绝安装。手动安装开发包不会获得“市场已审核”标记。

## 密钥轮换

1. 用旧密钥发布说明新指纹的版本或公告。
2. 在插件中心完成发布者密钥验证。
3. 新版本 Manifest 改用新完整指纹。
4. 保留旧公钥用于验证历史版本。
5. 密钥泄露时立即吊销，并停止分发所有受影响版本。
