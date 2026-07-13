# 兼容与废弃

> SDK `0.1.0-alpha.1`

0.1 alpha 允许调整公共 ABI，但变更会记录在 Release Notes。达到 1.0 后：

- 同一 Major 不删除或修改已有公共成员。
- 不给已有接口直接增加成员。
- 新服务使用新接口或新 Service ID。
- 废弃 API 提供替代方案并经过 Deprecated、Legacy、Removed 阶段。

插件应声明 API、Host、Service 和 UI Surface 的上下界。
