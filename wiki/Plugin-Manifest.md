# Plugin Manifest

> SDK `0.1.0-alpha.4`

`.pnp` 根目录的 `plugin.json` 是插件的机器可读契约。它描述身份、入口、版本范围、依赖、服务、权限、UI、数据迁移和签名。构建器会规范化 Manifest，Analyzer、打包器和运行时会分别验证它。

完整 JSON Schema 位于 [`schemas/plugin.schema.json`](https://github.com/MuXue1230-owo/PCL-N-Plugin-SDK/blob/main/schemas/plugin.schema.json)。根对象禁止未知字段，字段拼错会直接失败。

## 最小可加载 Manifest

```json
{
  "formatVersion": 1,
  "manifestVersion": 1,
  "id": "dev.example.plugin",
  "name": "Example Plugin",
  "version": "0.1.0",
  "publisher": {
    "id": "github:example",
    "namespace": "dev.example"
  },
  "license": "Apache-2.0",
  "entryPoint": {
    "assembly": "lib/net10.0/Example.Plugin.dll",
    "type": "Example.Plugin.EntryPlugin"
  },
  "api": {
    "minimum": "0.1",
    "maximumExclusive": "1.0"
  },
  "host": {
    "minimumVersion": "0.1.0"
  },
  "signing": {
    "fingerprint": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA"
  }
}
```

## 字段总览

### 身份与展示

| 字段 | 必需 | 说明 |
|---|---:|---|
| `formatVersion` | 是 | `.pnp` 容器格式，当前为 `1` |
| `manifestVersion` | 是 | Manifest Schema 版本，当前为 `1` |
| `id` | 是 | 全局插件 ID，建议反向域名 |
| `name` | 是 | 用户可见名称，最长 128 字符 |
| `version` | 是 | SemVer 2.0，必须与项目 `Version` 一致 |
| `channel` | 否 | `stable`、`beta`、`alpha` 或 `nightly` |
| `summary` | 否 | 最长 256 字符的市场摘要 |
| `description` | 否 | 完整说明 |
| `publisher` | 是 | 发布者身份与拥有的命名空间 |
| `authors` | 否 | 作者列表 |
| `license` | 是 | SPDX 表达式或明确许可证名称 |
| `homepage` / `source` / `issues` | 否 | 完整 URI |
| `icon` | 否 | 包内安全相对路径 |
| `categories` / `tags` | 否 | 市场分类与检索标签 |

发布者示例：

```json
"publisher": {
  "id": "github:MuXue1230-owo",
  "namespace": "dev.muxue"
},
"authors": [
  {
    "name": "Example Author",
    "id": "github:example",
    "url": "https://github.com/example"
  }
]
```

插件 `id` 应位于发布者 namespace 下，例如 `dev.muxue.tools`。

### 入口

```json
"entryPoint": {
  "assembly": "lib/net10.0/Example.Plugin.dll",
  "type": "Example.Plugin.EntryPlugin"
}
```

- 程序集必须位于 `lib/net10.0/`。
- `type` 使用完整 CLR 类型名，区分大小写。
- 入口必须实现 `IPclNPlugin` 并提供公开无参数构造函数。

### API 与 Host

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

`api` 使用 `major.minor`；`host` 使用 SemVer。上界均不包含自身。

### 平台

纯托管 AnyCPU 插件通常不需要 `platforms`。携带原生库时应声明：

```json
"platforms": {
  "operatingSystems": ["windows", "linux", "macos"],
  "architectures": ["x64", "arm64"],
  "runtimeIdentifiers": ["win-x64", "linux-x64", "osx-arm64"]
}
```

### 插件依赖与不兼容

```json
"dependencies": [
  { "id": "dev.example.core", "version": ">=1.0.0 <2.0.0", "kind": "required" },
  { "id": "dev.example.theme", "version": ">=0.5.0", "kind": "optional" }
],
"incompatibilities": [
  {
    "id": "dev.example.legacy",
    "version": "<2.0.0",
    "reason": "请升级 Legacy 插件到 2.0.0 以上。"
  }
]
```

细节见 [依赖与兼容声明](Dependencies-and-Compatibility)。

### 服务与权限

```json
"services": {
  "required": {
    "pcl.commands": ">=0.1 <1.0"
  },
  "optional": {
    "pcl.notifications": ">=0.1 <1.0"
  }
},
"permissions": [
  {
    "id": "pcl.commands",
    "reason": "注册用户可以主动执行的命令。"
  },
  {
    "id": "pcl.notifications",
    "reason": "显示命令完成提示。"
  }
]
```

required 服务不可用时插件不会启动；optional 服务需要代码主动降级。每个权限都应给出面向用户的具体原因。

### UI

```json
"ui": {
  "api": { "minimum": "0.1", "maximumExclusive": "1.0" },
  "avalonia": { "minimum": "12.0", "maximumExclusive": "13.0" },
  "requiresRestart": false,
  "targets": [
    {
      "target": "pcl.page.launch",
      "surface": ">=3.0 <4.0",
      "access": ["inject"],
      "operations": [
        {
          "id": "example-panel",
          "kind": "inject",
          "slot": "primary-actions.after",
          "axaml": "ui/ExamplePanel.axaml",
          "command": "dev.example.plugin.open",
          "priority": 0,
          "required": false,
          "fallback": "disable-feature"
        }
      ]
    }
  ]
}
```

安全路径只能使用 `/`，不能是绝对路径、不能含 `..` 或反斜杠。AXAML 必须位于 `ui/`，扩展名为 `.axaml`，且不能使用 `x:Class`。

详见 [UI Surface 与 Slot](UI-Surfaces-and-Slots) 和 [UI Patch 与冲突](UI-Patches-and-Conflicts)。

### 数据、激活与更新

```json
"data": {
  "schemaVersion": 2,
  "minimumReadableSchema": 1
},
"activation": {
  "mode": "startup"
},
"update": {
  "allowAutomaticUpdate": true,
  "requiresRestart": false
}
```

- `data` 用于声明插件持久数据的兼容窗口；迁移逻辑仍由插件实现。
- `activation.mode` 当前可为 `startup` 或 `on-demand`。
- `update` 描述自动更新和重启需求。

### 签名

```json
"signing": {
  "fingerprint": "0123456789ABCDEF0123456789ABCDEF01234567"
}
```

必须使用完整 OpenPGP 指纹，不能写短 Key ID。正式 `.pnp` 的签名、公钥与该指纹必须一致。

## 让 Analyzer 检查 Manifest

```xml
<ItemGroup>
  <AdditionalFiles Include="plugin.json" />
</ItemGroup>
```

这会启用 PNPSDK007–009。打包器和运行时仍会进行更完整的安全验证，因此编译通过不代表任意手工修改后的 `.pnp` 都能加载。

## 常见错误

- `id` 使用大写、空格或下划线；
- `PclNPluginId`、程序集版本和 Manifest 不一致；
- `entryPoint.type` 忘记 namespace；
- 服务写在 `required`，代码却没有真正使用，导致不必要的兼容限制；
- 声明 UI 操作却遗漏对应权限；
- 使用 Windows 反斜杠路径；
- 发布后覆盖同一个版本号。
