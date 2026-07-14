# 身份与版本

> SDK `0.1.0-alpha.5`

## 插件 ID

插件 ID 必须匹配：

```regex
^[a-z0-9]+([.-][a-z0-9]+)*$
```

推荐反向域名：

- `dev.example.tools`
- `com.example.launch-helper`
- `io.github.username.backup`

禁止大写、空格、下划线、连续分隔符，以及以 `.`/`-` 开头或结尾。

`pcl.*`、`official.*`、`system.*`、`internal.*` 为保留命名空间。插件中心会校验插件 ID 是否位于发布者拥有的 namespace 下。

ID 一旦发布就不要更改。改 ID 会被视为全新插件，用户设置、依赖和更新关系不会自动迁移。

## 三处必须一致

```xml
<PclNPluginId>dev.example.tools</PclNPluginId>
<Version>1.2.3</Version>
```

```json
"id": "dev.example.tools",
"version": "1.2.3"
```

构建输出会使用相同身份：

```text
dev.example.tools-1.2.3.pnp
```

## 插件版本

插件使用 SemVer 2.0：`MAJOR.MINOR.PATCH[-PRERELEASE][+BUILD]`。

- `1.0.0`：稳定版本；
- `1.1.0`：兼容新增功能；
- `1.1.1`：兼容修复；
- `2.0.0`：破坏性变化；
- `2.0.0-beta.1`：预发布；
- `1.2.3+build.42`：构建元数据，不影响优先级。

`PluginVersion.CompareTo` 按 SemVer 优先级比较；build metadata 不改变排序。

## API 与服务版本

插件版本、SDK NuGet 版本、Host API 版本和服务版本彼此独立：

| 类型 | 示例 | 格式 |
|---|---|---|
| 插件版本 | `1.2.3-beta.1` | SemVer 2.0 |
| SDK 包版本 | `0.1.0-alpha.5` | NuGet/SemVer |
| Plugin API | `0.2` | `major.minor` |
| 服务版本 | `0.1` | `major.minor` 范围 |
| UI Surface | `3.1` | 独立 Surface 版本 |

不要把 SDK `0.1.0-alpha.5` 写进 `api.minimum` 或服务范围。
