namespace PCL.N.Plugin;

public interface IPclNPlugin
{
    ValueTask InitializeAsync(
        IPluginContext context,
        CancellationToken cancellationToken);

    ValueTask ShutdownAsync(CancellationToken cancellationToken);
}

public interface IPluginContext
{
    PluginDescriptor Plugin { get; }

    PluginApiVersion ApiVersion { get; }

    /// <summary>Stable host services (settings store, commands, …). See design §7.7 / §9.</summary>
    IPluginServiceProvider Services { get; }

    /// <summary>Legacy/capability surface (settings pages, future UI capabilities).</summary>
    IPluginCapabilityProvider Capabilities { get; }

    IPluginLifetime Lifetime { get; }

    IPluginLogger Logger { get; }

    IPluginDispatcher Dispatcher { get; }

    PluginDirectorySet Directories { get; }

    CancellationToken Stopping { get; }
}

public interface IPluginCapabilityProvider
{
    bool TryGet<TCapability>(out TCapability? capability)
        where TCapability : class, IPluginCapability;
}

public interface IPluginCapability
{
    string Id { get; }

    PluginApiVersion Version { get; }
}

public interface IPluginRegistration : IAsyncDisposable
{
    string Id { get; }

    bool IsActive { get; }
}

public interface IPluginLifetime
{
    CancellationToken Stopping { get; }

    void Track(IPluginRegistration registration);

    void Track(IDisposable disposable);

    void Track(IAsyncDisposable disposable);
}
