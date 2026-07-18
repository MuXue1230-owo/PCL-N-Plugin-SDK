namespace PCL.N.Plugin;

public enum PluginSettingsHintKind
{
    Information,
    Warning,
    Error
}

[Obsolete("Use PluginLocalizedSettingsHintDescriptor from PCLN.Plugin.i18n.")]
public sealed record PluginSettingsHintDescriptor(
    string Text,
    PluginSettingsHintKind Kind = PluginSettingsHintKind.Information);

[Obsolete("Use PluginLocalizedSettingsPageGroupDescriptor from PCLN.Plugin.i18n.")]
public sealed record PluginSettingsPageGroupDescriptor(
    string Id,
    string Title,
    string? Icon = null,
    int Order = 500,
    string? Description = null);

[Obsolete("Use PluginLocalizedSettingsPageDescriptor from PCLN.Plugin.i18n.")]
public sealed record PluginSettingsPageDescriptor(
    string Id,
    string Title,
    string Icon,
    string Heading,
    string Description,
    IReadOnlyList<PluginSettingsHintDescriptor> Hints)
{
    public string? GroupId { get; init; }

    public int Order { get; init; } = 500;

    public bool RequiresDeveloperMode { get; init; }
}

[Obsolete("Use IPluginLocalizedSettingsPageGroupCapability from PCLN.Plugin.i18n.")]
public interface IPluginSettingsPageGroupCapability : IPluginCapability
{
    IPluginRegistration Register(PluginSettingsPageGroupDescriptor descriptor);
}

[Obsolete("Use IPluginLocalizedSettingsPageCapability from PCLN.Plugin.i18n.")]
public interface IPluginSettingsPageCapability : IPluginCapability
{
    IPluginRegistration Register(PluginSettingsPageDescriptor descriptor);
}
