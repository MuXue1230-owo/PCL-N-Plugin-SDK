namespace PCL.N.Plugin;

/// <summary>Stable host target identifier such as <c>pcl.page.launch</c>.</summary>
public readonly record struct UiTargetId
{
    public UiTargetId(string value)
    {
        if (!PluginId.TryParse(value, out _))
            throw new ArgumentException("UI target ID is invalid.", nameof(value));
        Value = value;
    }

    public string Value { get; }

    public bool IsDefault => Value is null;

    public override string ToString() => Value ?? string.Empty;
}

/// <summary>Metadata for a plugin-owned main navigation page.</summary>
public sealed record PluginPageDescriptor
{
    public PluginPageDescriptor(
        string operationId,
        string route,
        string title,
        string? icon = null,
        int order = 1000)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(route);
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        OperationId = operationId.Trim();
        Route = route.Trim();
        Title = title.Trim();
        Icon = icon;
        Order = order;
    }

    public string OperationId { get; init; }

    public string Route { get; init; }

    public string Title { get; init; }

    public string? Icon { get; init; }

    public int Order { get; init; }
}

/// <summary>Host navigation for pages registered through a UI adapter.</summary>
public interface IPluginNavigationService : IPluginService
{
    ValueTask NavigateAsync(string route, CancellationToken cancellationToken = default);
}

public static class PluginUiServiceIds
{
    public static PluginServiceId Navigation { get; } = new("pcl.navigation");

    public static PluginServiceId AvaloniaAccess { get; } = new("pcl.ui.avalonia");

    public static PluginServiceId AvaloniaPages { get; } = new("pcl.ui.avalonia.pages");

    public static PluginServiceId AvaloniaWindows { get; } = new("pcl.ui.avalonia.windows");
}
