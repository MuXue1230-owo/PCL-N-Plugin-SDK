# 测试插件

> SDK `0.1.0-alpha.4`

测试项目引用 `PCLN.Plugin.Testing`，通过 `TestPluginContext` 注入服务和 capability。

```csharp
await using TestPluginContext context = new(descriptor, new PluginApiVersion(0, 1));
context.TestServices.Add<IPluginCommandService>(new TestPluginCommandService());
await plugin.InitializeAsync(context, CancellationToken.None);
```

内存实现覆盖通知、设置、命令和实例查询。释放 context 会取消 stopping token 并逆序释放注册项。
