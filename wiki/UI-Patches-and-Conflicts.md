# UI Patch 与冲突

> SDK `0.1.0-alpha.1`

支持 `observe`、`register`、`inject`、`modify`、`replace`、`remove`、`reorder`、资源/样式/模板覆写、输入拦截和 `wrap` 声明。

排序依据：插件依赖、`before/after`、操作类型、priority、插件 ID、操作 ID。`replace` 默认独占；高风险操作修改相同目标时要求兼容声明并接受冲突计算。

AXAML 适用于 `register`、`inject`、`replace` 和 `wrap`。`modify` 通过 selector/propertyPath 声明，不接受 AXAML 文件。
