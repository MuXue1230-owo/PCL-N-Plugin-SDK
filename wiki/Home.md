# PCL N Plugin SDK

> 适用于 SDK `0.1.0-alpha.2`。当前为预发布版本，1.0 前公共 API 仍可能调整。

PCL N Plugin SDK 是第三方插件的公开开发入口。插件运行时 `PCL.Plugin` 保持私有；插件不得引用 `PCL.Application`、`PCL.Desktop` 或 `PCL.Plugin`。

## 开始

1. 阅读 [[Getting-Started]]。
2. 了解五个 [[NuGet-Packages]]。
3. 编写 [[Plugin-Manifest]]。
4. 使用 [[Building-PNP-Packages]] 生成 `.pnp`。
5. 正式发布前完成 [[OpenPGP-Signing]]。

## 重要文档

- [[Architecture-and-Boundaries]]
- [[Services]]
- [[UI-Surfaces-and-Slots]]
- [[UI-Patches-and-Conflicts]]
- [[Analyzer-Reference]]
- [[Troubleshooting]]
