# 快速开始

> SDK `0.1.0-alpha.1`

创建 `net10.0` 类库并安装：

```powershell
dotnet add package PCL.N.Plugin.Abstractions --version 0.1.0-alpha.1
dotnet add package PCL.N.Plugin.Sdk --version 0.1.0-alpha.1
dotnet add package PCL.N.Plugin.Analyzers --version 0.1.0-alpha.1
dotnet add package PCL.N.Plugin.Sdk.Build --version 0.1.0-alpha.1
```

实现入口：

```csharp
using PCL.N.Plugin;

public sealed class ExamplePlugin : IPclNPlugin
{
    public ValueTask InitializeAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        context.Logger.Info("Example plugin initialized.");
        return ValueTask.CompletedTask;
    }

    public ValueTask ShutdownAsync(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}
```

项目中放置 `plugin.json`，并设置：

```xml
<PropertyGroup>
  <PclNPluginId>dev.example.plugin</PclNPluginId>
  <PclNPluginSign>false</PclNPluginSign>
</PropertyGroup>
<ItemGroup>
  <AdditionalFiles Include="plugin.json" />
</ItemGroup>
```

运行 `dotnet build -c Release`。开发构建可暂时不签名，但未签名 `.pnp` 不得正式分发。完整示例位于公开仓库的 `examples/HelloPlugin`。
