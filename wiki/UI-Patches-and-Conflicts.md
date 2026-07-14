# UI Patch 与冲突

> SDK `0.1.0-alpha.4`

UI Patch 用于描述插件对稳定 Surface 的操作。支持：`observe`、`register`、`inject`、`modify`、`replace`、`remove`、`reorder`、资源/样式/模板覆写、输入拦截和 `wrap`。

## Patch 声明

```json
{
  "id": "launch-title",
  "kind": "modify",
  "selector": "primary-actions.launch-button",
  "propertyPath": "Content",
  "priority": 20,
  "required": false,
  "fallback": "skip-patch",
  "modifyPolicy": "last-writer"
}
```

`modify` 通过 selector/propertyPath 声明，不接受 AXAML 文件。`register`、`inject`、`replace` 和 `wrap` 可以引用 AXAML。

## 排序规则

Host 综合以下信息生成确定顺序：

1. 插件 required 依赖关系；
2. `before` / `after`；
3. 操作类型；
4. `priority`；
5. 插件 ID；
6. 操作 ID。

```json
"before": ["dev.example.other:*"],
"after": ["dev.example.base:prepare"]
```

全局 Patch ID 是 `pluginId:operationId`。不要只靠高 priority 解决语义依赖；明确使用 `before`/`after`。

## 冲突类型

- 两个独占 `replace` 指向同一目标；
- 两个高风险操作组合；
- `modify` 同一属性且策略为 exclusive；
- `before`/`after` 构成环；
- Surface 或 Slot 不支持目标操作。

`replace` 默认独占。需要允许其他插件包裹替换结果时，显式设置 `allowWrapping: true`，并让对方使用 `wrap`。

## Modify 冲突策略

| 策略 | 行为 |
|---|---|
| `exclusive` | 同一属性只允许一个写入者 |
| `first-writer` | 第一项生效 |
| `last-writer` | 最后一项生效 |
| `merge` | 合并可合并值 |
| `chain` | 按顺序应用转换 |
| `custom` | 需要宿主支持的自定义解决器 |

只有在属性语义确实支持时才使用 `merge` 或 `chain`。

## 代码注册 Patch

```csharp
IPluginUiPatchService patches = context.Services.Require<IPluginUiPatchService>();

context.Lifetime.Track(patches.Register(new PluginUiPatchDescriptor(
    operationId: "launch-title",
    target: "pcl.page.launch",
    kind: PluginUiPatchKind.Modify,
    surfaceVersionRange: ">=3.0 <4.0",
    slot: "primary-actions.launch-button",
    priority: 20,
    required: false,
    fallback: PluginUiPatchFallback.SkipPatch,
    propertyPath: "Content",
    modifyPolicy: PluginUiModifyConflictPolicy.LastWriter)));

PluginUiPatchPlan plan = patches.Plan();
foreach (PluginUiConflict conflict in plan.Conflicts)
    context.Logger.Warn(conflict.Message);
```

## 安全模式

UI Safe Mode 会跳过高风险 modify、replace 和 Raw UI 类操作，让用户在插件冲突后仍能进入启动器处理问题。不要把数据迁移或关键业务副作用放进 UI Patch 回调。
