# 生命周期与注册项

> SDK `0.1.0`

一个插件实例只经历一次初始化和一次停止。宿主负责验证包、创建隔离加载上下文、提供服务、释放注册项，并尝试卸载程序集。

## 生命周期顺序

```text
验证 Manifest、签名、依赖和服务
  → 创建插件实例
  → InitializeAsync
  → Active
  → 取消 context.Stopping
  → ShutdownAsync
  → 逆序释放 Lifetime 中的注册项
  → 卸载 AssemblyLoadContext
```

## 推荐入口结构

```csharp
public sealed class ExamplePlugin : IPclNPlugin
{
    public async ValueTask InitializeAsync(
        IPluginContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IPluginCommandService commands = context.Services.Require<IPluginCommandService>();
        context.Lifetime.Track(commands.Register(new PluginCommandDescriptor(
            "dev.example.plugin.run",
            "运行示例",
            token => RunAsync(context, token))));

        await WarmUpAsync(cancellationToken);
        context.Logger.Info("初始化完成。");
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken)
    {
        // 只处理插件自身尚未交给 Lifetime 的状态。
        return ValueTask.CompletedTask;
    }
}
```

## 所有注册都必须 Track

改变宿主状态的 API 返回 `IPluginRegistration`。立即将结果交给 Lifetime：

```csharp
context.Lifetime.Track(commands.Register(descriptor));
context.Lifetime.Track(pages.Register(page));
context.Lifetime.Track(tasks.SchedulePeriodic(id, interval, callback));
```

你也可以跟踪普通资源：

```csharp
context.Lifetime.Track((IDisposable)watcher);
context.Lifetime.Track((IAsyncDisposable)connection);
```

插件停止时按注册的逆序释放，适合先注册底层资源、后注册依赖它们的 UI 和命令。

## 取消 Token

- `InitializeAsync` 的 Token：宿主取消本次加载。
- `context.Stopping`：插件生命周期即将结束，适合长期循环。
- `ShutdownAsync` 的 Token：宿主要求停止过程结束。
- `IPluginTaskService` 回调 Token：任务注册被释放或插件停止。

所有异步 I/O 都应传递正确的 Token。

## 不要创建失控后台工作

Analyzer PNPSDK010 会阻止常见的未跟踪后台工作：

- `Task.Run`；
- `new Thread(...)`；
- `Timer`；
- `FileSystemWatcher`；
- 不能随插件停止的 fire-and-forget Task。

优先使用 `IPluginTaskService.Run` 或 `SchedulePeriodic`。确需持有 `IDisposable`/`IAsyncDisposable` 时，将其交给 Lifetime，并保证回调能停止。

## 初始化失败

`InitializeAsync` 抛出异常时，Host 会：

1. 标记加载失败并记录错误；
2. 释放已经 Track 的注册项；
3. 卸载插件加载上下文；
4. 不让半初始化插件继续运行。

因此不要为了“看起来加载成功”而吞掉关键异常。只有确实可降级的可选功能才应捕获、记录并继续。
