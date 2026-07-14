# 依赖与兼容声明

> SDK `0.1.0-alpha.5`

插件依赖、宿主兼容和 UI 兼容是三个不同层次。把它们写清楚，运行时才能在加载前给出可操作的错误，而不是在插件代码中随机失败。

## 插件前置依赖

```json
"dependencies": [
  {
    "id": "dev.example.core",
    "version": ">=1.2.0 <2.0.0",
    "kind": "required"
  },
  {
    "id": "dev.example.integration",
    "version": ">=0.5.0",
    "kind": "optional"
  }
]
```

| `kind` | 行为 |
|---|---|
| `required` | 缺失、禁用或版本不匹配时，当前插件不会加载 |
| `optional` | 不阻止加载；插件必须自行检测相应服务或能力并降级 |

运行时会计算依赖拓扑并先加载 required 前置。依赖图出现环时无法得到安全加载顺序，应拆分共同能力或重新设计依赖方向。

### 版本范围

插件版本范围使用 SemVer 2.0：

| 范围 | 含义 |
|---|---|
| `1.2.3` | 只接受 1.2.3 |
| `>=1.2.0` | 接受 1.2.0 及以上 |
| `>=1.2.0 <2.0.0` | 接受 1.x 且至少为 1.2.0 |
| `<2.0.0` | 接受 2.0.0 之前的版本 |

不要使用没有上界的 required 范围来假设未来 Major 一定兼容。

## 不兼容声明

```json
"incompatibilities": [
  {
    "id": "dev.example.legacy-ui",
    "version": "<2.0.0",
    "reason": "两者会独占替换启动页主操作区。"
  }
]
```

声明是对称执行的：无论新安装的插件还是已安装的插件声明冲突，只要版本匹配，运行时都会阻止冲突组合继续启用，并记录兼容性观测。

`reason` 应说明用户能理解的后果和解决方式，例如“请升级到 2.0.0 以上”，不要只写“冲突”。

## Host 与 API 范围

```json
"api": {
  "minimum": "0.1",
  "maximumExclusive": "1.0"
},
"host": {
  "minimumVersion": "0.1.0",
  "maximumVersionExclusive": "2.0.0"
}
```

- `api` 约束插件公共 ABI，格式是 `major.minor`。
- `host` 约束 PCL N/PCL.Plugin 实现版本，格式是 SemVer。
- `maximumExclusive` 是开区间，不包含该版本。

除非已经确认某个未来 Host 会破坏插件，不要随意设置过窄的 `host.maximumVersionExclusive`；公共 API 的上界通常更重要。

## 服务范围

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0"
  },
  "optional": {
    "pcl.notifications": ">=0.1 <1.0"
  }
}
```

required 服务会在插件实例化前验证。optional 服务必须使用 `TryGet`：

```csharp
if (context.Services.TryGet<IPluginNotificationService>(out var notifications))
    notifications.ShowInformation("可选通知服务可用。");
else
    context.Logger.Info("通知服务不可用，已降级为日志。");
```

## UI Surface 范围

```json
"ui": {
  "api": { "minimum": "0.1", "maximumExclusive": "1.0" },
  "targets": [
    {
      "target": "pcl.page.launch",
      "surface": ">=3.0 <4.0",
      "access": ["inject"]
    }
  ]
}
```

UI Surface 独立版本化。不要把 PCL N 的产品版本当作 Surface 版本，也不要依赖控件名称、Visual Tree 索引或本地化文本。

## 发布前检查

- required 依赖都已经公开可获取；
- 依赖范围能覆盖你实际测试过的版本；
- optional 依赖缺失时功能仍能正常降级；
- 没有依赖环；
- 不兼容声明包含明确原因；
- API、Host、服务和 UI Surface 范围没有互相矛盾。
