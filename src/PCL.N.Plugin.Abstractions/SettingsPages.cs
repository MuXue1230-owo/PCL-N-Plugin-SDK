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

public sealed record PluginSettingsPageGroupDescriptor(
    string Id,
    string Title,
    string? Icon = null,
    int Order = 500,
    string? Description = null);

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

public interface IPluginSettingsPageGroupCapability : IPluginCapability
{
    IPluginRegistration Register(PluginSettingsPageGroupDescriptor descriptor);
}

public interface IPluginSettingsPageCapability : IPluginCapability
{
    IPluginRegistration Register(PluginSettingsPageDescriptor descriptor);
}
