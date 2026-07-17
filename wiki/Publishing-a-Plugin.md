# 发布插件

> Applies to PCL N Plugin SDK 0.2.0.

> SDK `0.2.0`

上传物是不可变的、由开发者正式密钥自签名的候选 `.pnp`。不要上传普通 DLL、Debug 输出或 SDK 本机开发密钥签名包。

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

插件中心还要求版本按 SemVer 严格递增：不能上传低于或等于任一历史上传版本的版本号，仅改变 `+build` 元数据也不算递增。新版本发布后，旧版本只在网站保留 14 天回退下载时间；到期会删除旧候选包和网站签名分发包，但保留版本、哈希、签名和审核记录。因此长期归档应由发布者自行保存，不能把插件中心当作无限期制品仓库。

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

市场资料与包内 Manifest 是两套互补信息：`manifest.icon` 随 `.pnp` 分发，供启动器离线识别；市场图标由开发者工作台单独上传，只用于商店展示。市场图标支持 PNG、JPEG、WebP，最大 2 MiB，替换市场图标不会改变已签名插件包。

首次创建或再次编辑插件时，必须同时填写 `zh-CN` 与 `en-US` 的名称、简短说明和详细说明。两种语言任一缺失都不能送审；旧版已发布条目仍可按中文回退读取，但下一次编辑或发布前必须补齐英文。

1. 打开 [PCL N 插件中心](https://pcln.top/)。
2. 使用 GitHub 或 Microsoft 登录。
3. 创建/加入发布组织，确认 publisher namespace。
4. 创建插件条目并填写市场资料。
5. 上传新的 `.pnp` 版本和变更说明。
6. 等待自动扫描与人工审核。
7. 审核通过后，网站自动生成并签名新的市场分发 `.pnp`；只有生成、上传和发布记录全部成功后才公开。

网站分发包同时保留开发者签名，并追加网站签名封套。桌面端会验证两层 OpenPGP 签名、哈希、Manifest 和兼容范围，再向网站在线核对最终包 SHA-256、发布者、命名空间和吊销状态；市场审核不能替代运行时安全检查。

## 发布后验证

- 商店详情页显示正确版本、分类、权限和前置；
- 匿名用户可以浏览公开详情；
- 登录用户能获得正确的下载/购买状态；
- PCL N 桌面市场能安装并启用；
- required 前置加载顺序正确；
- 老版本升级后设置和数据仍可读取；
- GitHub Release 的开发者候选包 SHA-256 与插件中心记录的候选哈希相同；插件中心追加网站签名后，最终分发包 SHA-256 必然不同，并应以网站验证接口返回值为准。