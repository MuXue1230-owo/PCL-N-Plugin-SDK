namespace PCL.N.Plugin;

public enum PluginSettingsHintKind
{
    Information,
    Warning,
    Error
}

public sealed record PluginSettingsHintDescriptor(
    string Text,
    PluginSettingsHintKind Kind = PluginSettingsHintKind.Information);

public sealed record PluginSettingsPageDescriptor(
    string Id,
    string Title,
    string Icon,
    string Heading,
    string Description,
    IReadOnlyList<PluginSettingsHintDescriptor> Hints);

public interface IPluginSettingsPageCapability : IPluginCapability
{
    IPluginRegistration Register(PluginSettingsPageDescriptor descriptor);
}
