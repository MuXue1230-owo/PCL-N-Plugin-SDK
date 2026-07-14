# 生命周期与注册项

> SDK `0.1.0-alpha.2`

插件实现 `IPclNPlugin.InitializeAsync` 和 `ShutdownAsync`。所有改变宿主状态的服务返回 `IPluginRegistration`，必须交给 `context.Lifetime.Track(...)`。

```csharp
IPluginRegistration registration = commands.Register(descriptor);
context.Lifetime.Track(registration);
```

宿主停止插件时会取消 `context.Stopping`，然后逆序释放注册项。后台工作应使用 `IPluginTaskService`，不要直接创建无法追踪的 Thread、Timer、`Task.Run` 或 `FileSystemWatcher`。
