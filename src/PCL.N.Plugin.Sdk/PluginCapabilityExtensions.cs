namespace PCL.N.Plugin.Sdk;

public static class PluginCapabilityExtensions
{
    public static TCapability Require<TCapability>(this IPluginCapabilityProvider capabilities)
        where TCapability : class, IPluginCapability
    {
        ArgumentNullException.ThrowIfNull(capabilities);
        return capabilities.TryGet(out TCapability? capability) && capability is not null
            ? capability
            : throw new NotSupportedException(
                $"The host does not provide plugin capability {typeof(TCapability).FullName}.");
    }

    public static TRegistration Track<TRegistration>(
        this IPluginLifetime lifetime,
        TRegistration registration)
        where TRegistration : class, IPluginRegistration
    {
        ArgumentNullException.ThrowIfNull(lifetime);
        ArgumentNullException.ThrowIfNull(registration);
        lifetime.Track(registration);
        return registration;
    }
}
