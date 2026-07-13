# Plugin Manifest

> SDK `0.1.0-alpha.1`

根目录 `plugin.json` 声明插件身份、入口、兼容范围、服务、权限和 UI 操作。完整 Schema 位于公开仓库 `schemas/plugin.schema.json`。

最小结构：

```json
{
  "formatVersion": 1,
  "manifestVersion": 1,
  "id": "dev.example.plugin",
  "name": "Example",
  "version": "0.1.0",
  "publisher": { "id": "github:example", "namespace": "dev.example" },
  "license": "Apache-2.0",
  "entryPoint": { "assembly": "lib/net10.0/Example.dll", "type": "Example.Plugin" },
  "api": { "minimum": "0.1", "maximumExclusive": "1.0" },
  "host": { "minimumVersion": "0.1.0" },
  "signing": { "fingerprint": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" }
}
```

`ui.targets[].operations[].axaml` 可引用包内 `ui/*.axaml`。路径不能包含 `..`、反斜杠或绝对路径。AXAML 不允许 `x:Class` 或启动器私有 CLR 命名空间。
