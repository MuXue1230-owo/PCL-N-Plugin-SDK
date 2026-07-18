namespace PCL.N.Plugin;

public sealed record PluginLocalizedSettingsHintDescriptor(
    PclLocalizedString Text,
    PluginSettingsHintKind Kind = PluginSettingsHintKind.Information);

public sealed record PluginLocalizedSettingsPageGroupDescriptor(
    string Id,
    PclLocalizedString Title,
    string? Icon = null,
    int Order = 500,
    PclLocalizedString? Description = null);

public sealed record PluginLocalizedSettingsPageDescriptor(
    string Id,
    PclLocalizedString Title,
    string Icon,
    PclLocalizedString Heading,
    PclLocalizedString Description,
    IReadOnlyList<PluginLocalizedSettingsHintDescriptor> Hints)
{
    public string? GroupId { get; init; }

    public int Order { get; init; } = 500;

    public bool RequiresDeveloperMode { get; init; }
}

public interface IPluginLocalizedSettingsPageGroupCapability : IPluginCapability
{
    IPluginRegistration Register(PluginLocalizedSettingsPageGroupDescriptor descriptor);
}

public interface IPluginLocalizedSettingsPageCapability : IPluginCapability
{
    IPluginRegistration Register(PluginLocalizedSettingsPageDescriptor descriptor);
}
