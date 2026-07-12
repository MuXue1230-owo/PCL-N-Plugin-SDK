namespace PCL.Plugin.Sdk;

public sealed record HostSettingsHint(
    string Text,
    HostSettingsHintKind Kind = HostSettingsHintKind.Information);

public enum HostSettingsHintKind
{
    Information,
    Warning,
    Error
}

public sealed record HostSettingsPage(
    string Id,
    string Title,
    string Icon,
    string Heading,
    string Description,
    IReadOnlyList<HostSettingsHint> Hints);

public interface IHostSettingsPageRegistry
{
    IReadOnlyList<HostSettingsPage> Pages { get; }

    void Add(HostSettingsPage page);
}

public sealed class HostSettingsPageRegistry : IHostSettingsPageRegistry
{
    private readonly List<HostSettingsPage> _pages = [];
    private readonly HashSet<string> _ids = new(StringComparer.OrdinalIgnoreCase);

    public IReadOnlyList<HostSettingsPage> Pages => _pages;

    public void Add(HostSettingsPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        ArgumentException.ThrowIfNullOrWhiteSpace(page.Id);
        ArgumentException.ThrowIfNullOrWhiteSpace(page.Title);
        if (!_ids.Add(page.Id))
            throw new InvalidOperationException($"Settings page already registered: {page.Id}");
        _pages.Add(page);
    }
}
