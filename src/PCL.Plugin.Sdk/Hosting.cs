namespace PCL.Plugin.Sdk;

public readonly record struct HostModuleId(string Value)
{
    public override string ToString() => Value ?? string.Empty;
}

public interface IPclHostModule
{
    HostModuleId Id { get; }

    void Configure(IPclHostBuilder builder);
}

public interface IPclHostBuilder
{
    Version ApiVersion { get; }

    TCapability? GetCapability<TCapability>()
        where TCapability : class;

    TCapability RequireCapability<TCapability>()
        where TCapability : class;
}

public sealed class PclHostBuilder : IPclHostBuilder
{
    private readonly Dictionary<Type, object> _capabilities = [];

    public PclHostBuilder(Version apiVersion)
    {
        ArgumentNullException.ThrowIfNull(apiVersion);
        ApiVersion = apiVersion;
    }

    public Version ApiVersion { get; }

    public PclHostBuilder AddCapability<TCapability>(TCapability capability)
        where TCapability : class
    {
        ArgumentNullException.ThrowIfNull(capability);
        if (!_capabilities.TryAdd(typeof(TCapability), capability))
            throw new InvalidOperationException($"Capability already registered: {typeof(TCapability).FullName}");
        return this;
    }

    public TCapability? GetCapability<TCapability>()
        where TCapability : class =>
        _capabilities.TryGetValue(typeof(TCapability), out object? capability)
            ? (TCapability)capability
            : null;

    public TCapability RequireCapability<TCapability>()
        where TCapability : class =>
        GetCapability<TCapability>() ?? throw new NotSupportedException(
            $"The host does not provide capability {typeof(TCapability).FullName}.");
}
