# 发布插件

> SDK `0.1.0-alpha.4`

发布物是不可变的、已签名的 `.pnp`。不要把普通 DLL、Debug 输出或未签名开发包作为正式版本上传。

## 发布前清单

- `dotnet build -c Release` 成功；
- 自动化测试全部通过；
- 项目 `Version` 与 Manifest `version` 完全相同；
- `.pnp` 使用正式 OpenPGP 密钥签名；
- API、Host、服务、权限、依赖和 UI Surface 范围准确；
- required 前置已经可获取；
- 包内没有 Abstractions 或启动器私有程序集；
- README、许可证、图标、变更说明和支持链接完整；
- 在当前稳定 PCL N 上完成一次安装、启用、停用和升级测试；
- 对付费插件确认价格、授权和退款提示。

## SemVer 建议

| 变化 | 示例 |
|---|---|
| 修复且兼容 | `1.2.3` → `1.2.4` |
| 新增兼容功能 | `1.2.3` → `1.3.0` |
| 破坏配置、命令或依赖契约 | `1.2.3` → `2.0.0` |
| 预发布 | `2.0.0-alpha.1`、`2.0.0-beta.1` |

已经发布的版本号不可覆盖。重新构建内容不同的同版本包会破坏审计和更新安全。

## GitHub Release 示例

```yaml
name: Release plugin

on:
  push:
    tags: ['v*']

permissions:
  contents: write

jobs:
  release:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v6
      - uses: actions/setup-dotnet@v5
        with:
          dotnet-version: 10.0.x
      - run: dotnet restore
      - run: dotnet test -c Release --no-restore
      - name: Import signing key
        env:
          PNP_GPG_PRIVATE_KEY: ${{ secrets.PNP_GPG_PRIVATE_KEY }}
        run: printf '%s' "$PNP_GPG_PRIVATE_KEY" | gpg --batch --import
      - run: dotnet build src/Example.Plugin/Example.Plugin.csproj -c Release --no-restore -p:PclNPluginSign=true -p:PclNPluginOutputDirectory=${{ github.workspace }}/artifacts/
      - uses: softprops/action-gh-release@v2
        with:
          files: artifacts/*.pnp
```

Tag 必须与 Manifest 版本对应。可以在脚本中解析 `plugin.json` 并拒绝不一致的 Tag。

## 发布到 PCL N 插件中心

1. 打开 [PCL N 插件中心](https://pcln.top/)。
2. 使用 GitHub 或 Microsoft 登录。
3. 创建/加入发布组织，确认 publisher namespace。
4. 创建插件条目并填写市场资料。
5. 上传新的 `.pnp` 版本和变更说明。
6. 等待自动扫描与人工审核。
7. 审核通过后发布到市场。

桌面端会再次验证包签名、哈希、Manifest 和兼容范围；市场审核不能替代运行时安全检查。

## 发布后验证

- 商店详情页显示正确版本、分类、权限和前置；
- 匿名用户可以浏览公开详情；
- 登录用户能获得正确的下载/购买状态；
- PCL N 桌面市场能安装并启用；
- required 前置加载顺序正确；
- 老版本升级后设置和数据仍可读取；
- GitHub Release 与插件中心的 SHA-256 相同。
