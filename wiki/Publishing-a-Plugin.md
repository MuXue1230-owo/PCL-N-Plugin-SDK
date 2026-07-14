# 发布插件

> SDK `0.1.0-alpha.4`

发布前确认：

- Release 构建成功。
- `.pnp` 使用正式密钥签名。
- Manifest、权限、UI Target 和 AXAML 路径完整。
- 包内没有 Abstractions 或启动器私有程序集。
- 测试宿主测试通过。
- 许可证、README 和变更说明已包含。

插件版本发布后不可覆盖；修复应发布新的 SemVer 版本。
