# 身份与版本

> SDK `0.1.0-alpha.1`

插件 ID 匹配：

```regex
^[a-z0-9]+([.-][a-z0-9]+)*$
```

推荐反向域名，例如 `dev.example.tools`。`pcl.*`、`official.*`、`system.*` 和 `internal.*` 为保留命名空间。

插件版本使用 SemVer 2.0。`PluginVersion.CompareTo` 按 SemVer 优先级比较，并忽略 build metadata 的优先级影响。API 和服务使用 `major.minor`，可通过 `PluginApiVersionRange` 表示 `>=0.1 <1.0`。

首版为 alpha，不承诺 1.0 级 ABI 稳定性。
