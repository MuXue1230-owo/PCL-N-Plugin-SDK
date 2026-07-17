namespace PCL.N.Plugin;

#pragma warning disable CA1711, CA1715 // Public names intentionally follow the required PclUiXXX convention.

/// <summary>A localized string rendered with the host-selected culture.</summary>
public sealed record PclUiString
{
    public PclUiString(string fallback, string? key = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);
        if (key is not null && string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Localization key cannot be empty.", nameof(key));
        Fallback = fallback;
        Key = key;
    }

    public string Fallback { get; init; }

    public string? Key { get; init; }

    public static PclUiString Localized(string key, string fallback) => new(fallback, key);

    public static implicit operator PclUiString(string value) => new(value);
}

public readonly record struct PclUiThickness(double Left, double Top, double Right, double Bottom)
{
    public PclUiThickness(double uniform) : this(uniform, uniform, uniform, uniform) { }

    public PclUiThickness(double horizontal, double vertical) : this(horizontal, vertical, horizontal, vertical) { }
}

public enum PclUiOrientation
{
    Vertical,
    Horizontal
}

public enum PclUiTextStyle
{
    Body,
    Caption,
    Subtitle,
    Title,
    Heading
}

public enum PclUiButtonStyle
{
    Normal,
    Primary,
    Danger,
    Subtle
}

public enum PclUiEventKind
{
    Click,
    ValueChanged,
    Submitted
}

/// <summary>Framework-neutral base element rendered by the launcher.</summary>
public abstract record PclUiElement
{
    public string? Id { get; init; }

    public PclUiThickness Margin { get; init; }

    public bool IsVisible { get; init; } = true;

    public bool IsEnabled { get; init; } = true;

    public double? Width { get; init; }

    public double? MinWidth { get; init; }

    public double? MaxWidth { get; init; }
}

public sealed record PclUiStack : PclUiElement
{
    public PclUiOrientation Orientation { get; init; } = PclUiOrientation.Vertical;

    public double Spacing { get; init; } = 8;

    public IReadOnlyList<PclUiElement> Children { get; init; } = [];
}

public sealed record PclUiCard : PclUiElement
{
    public required PclUiString Title { get; init; }

    public required PclUiElement Content { get; init; }

    public bool IsCollapsible { get; init; }
}

public sealed record PclUiText : PclUiElement
{
    public required PclUiString Text { get; init; }

    public PclUiTextStyle Style { get; init; } = PclUiTextStyle.Body;

    public bool Wrap { get; init; } = true;
}

public sealed record PclUiButton : PclUiElement
{
    public required PclUiString Text { get; init; }

    public PclUiButtonStyle Style { get; init; } = PclUiButtonStyle.Normal;

    public string? CommandId { get; init; }

    public PclUiString? ToolTip { get; init; }
}

public sealed record PclUiTextBox : PclUiElement
{
    public PclUiString? Label { get; init; }

    public PclUiString? Placeholder { get; init; }

    public string Value { get; init; } = string.Empty;

    public bool IsPassword { get; init; }

    public bool IsMultiline { get; init; }
}

public sealed record PclUiToggle : PclUiElement
{
    public required PclUiString Text { get; init; }

    public bool IsChecked { get; init; }
}

public sealed record PclUiOption(string Value, PclUiString Text);

public sealed record PclUiSelect : PclUiElement
{
    public PclUiString? Label { get; init; }

    public string? Value { get; init; }

    public IReadOnlyList<PclUiOption> Options { get; init; } = [];
}

public sealed record PclUiSlider : PclUiElement
{
    public PclUiString? Label { get; init; }

    public int Minimum { get; init; }

    public int Maximum { get; init; } = 100;

    public int Value { get; init; }

    public int Step { get; init; } = 1;
}

public sealed record PclUiProgress : PclUiElement
{
    public double Value { get; init; }

    public PclUiString? Text { get; init; }
}

public sealed record PclUiMarkdown : PclUiElement
{
    public required PclUiString Markdown { get; init; }
}

public sealed record PclUiSpacer : PclUiElement
{
    public double Size { get; init; } = 8;
}

public sealed record PclUiPage
{
    public required string OperationId { get; init; }

    public required string Route { get; init; }

    public required PclUiString Title { get; init; }

    public string? Icon { get; init; }

    public int Order { get; init; } = 1000;

    public required PclUiElement Content { get; init; }
}

public sealed record PclUiContribution
{
    public required string OperationId { get; init; }

    public required UiTargetId Target { get; init; }

    public required string Slot { get; init; }

    public required PclUiString Title { get; init; }

    public int Order { get; init; } = 1000;

    public required PclUiElement Content { get; init; }
}

public sealed class PclUiEventArgs(string elementId, PclUiEventKind kind, object? value = null) : EventArgs
{
    public string ElementId { get; } = string.IsNullOrWhiteSpace(elementId)
        ? throw new ArgumentException("Element ID is required for interactive events.", nameof(elementId))
        : elementId;

    public PclUiEventKind Kind { get; } = kind;

    public object? Value { get; } = value;
}

/// <summary>
/// High-level launcher-native UI service. Elements are rendered by the host with PCL N's
/// original controls and never expose launcher-private CLR types to plugins.
/// </summary>
public interface PclUiService : IPluginService
{
    event EventHandler<PclUiEventArgs>? EventRaised;

    IPluginRegistration RegisterPage(PclUiPage page);

    IPluginRegistration Inject(PclUiContribution contribution);

    ValueTask NavigateAsync(string route, CancellationToken cancellationToken = default);
}

public static class PclUiServiceIds
{
    public static PluginServiceId Components { get; } = new("pcl.ui.components");
}

#pragma warning restore CA1711, CA1715
