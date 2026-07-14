# UI Surface 与 Slot

> SDK `0.1.0-alpha.2`

Surface 是宿主发布的稳定 UI 边界，Target 是稳定目标，Slot 是稳定插入点。插件不能依赖控件类名、XAML Name、本地化文本或 Visual Tree 索引。

声明式 AXAML 示例：

```json
{
  "id": "hello-panel",
  "kind": "inject",
  "slot": "primary-actions.after",
  "axaml": "ui/HelloPanel.axaml",
  "command": "dev.example.hello"
}
```

AXAML 只描述 UI。事件行为通过公开命令 ID 或公开绑定上下文提供，不使用代码隐藏。宿主负责在 UI 线程解析、权限检查、资源隔离和撤销。
