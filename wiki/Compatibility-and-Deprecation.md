# 兼容与废弃

> SDK `0.1.0-alpha.4`

0.1 alpha 允许调整公共 ABI，变更会记录在 Release Notes。插件项目应固定 SDK 版本，并在升级前运行完整测试和桌面集成验证。

## 1.0 后的兼容原则

- 同一 Major 不删除或修改已有公共成员；
- 不直接给已发布接口增加必须实现的成员；
- 新能力使用新接口、新 Service ID 或可选 Capability；
- DTO 新增字段应提供兼容默认值；
- 废弃 API 经过 Deprecated → Legacy → Removed 阶段；
- Removed 只发生在新的 Major。

## 插件作者的责任

- 为 API、Host、服务和 UI Surface 声明上下界；
- 不引用私有 Host 类型；
- 对 optional 服务和 Capability 提供降级；
- 数据格式变更时维护 `data.schemaVersion` 与迁移；
- 命令 ID、设置 Key、插件 ID 和公开行为保持稳定；
- 升级 SDK 后重新测试安装、启用、停用和升级。

## 兼容测试矩阵

至少覆盖：

| 维度 | 建议 |
|---|---|
| Host | 最低支持版本、当前稳定版本 |
| 平台 | Manifest 声明的每个 OS/架构 |
| 依赖 | 最低兼容版本、最新同 Major 版本 |
| 数据 | 全新安装、从上一稳定版升级 |
| UI | Surface 存在、可选 Surface 缺失、安全模式 |

## 收到废弃警告时

1. 阅读 SDK Release Notes 和替代 API。
2. 先添加兼容新旧 Host 的适配层。
3. 提高最低 API/Host 版本时发布新的插件版本。
4. 删除旧路径前确认用户升级窗口已经结束。
