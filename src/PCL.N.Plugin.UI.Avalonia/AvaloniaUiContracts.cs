using Avalonia;
using Avalonia.Controls;

namespace PCL.N.Plugin;

/// <summary>UI-thread context supplied to an audited Raw Avalonia callback.</summary>
public interface IAvaloniaUiContext
{
    Application Application { get; }

    IReadOnlyList<TopLevel> TopLevels { get; }

    Control? ResolveControl(UiTargetId target);
}

/// <summary>Trusted, permission-gated access to real Avalonia objects (design §14.1).</summary>
public interface IAvaloniaUiAccessService : IPluginService
{
    Application Application { get; }

    IReadOnlyList<TopLevel> TopLevels { get; }

    ValueTask<Control?> ResolveControlAsync(
        UiTargetId target,
        CancellationToken cancellationToken = default);

    ValueTask<TResult> InvokeAsync<TResult>(
        Func<IAvaloniaUiContext, TResult> action,
        CancellationToken cancellationToken = default);
}

public interface IPluginUiDispatcher
{
    bool CheckAccess();

    ValueTask InvokeAsync(Action action, CancellationToken cancellationToken = default);

    ValueTask<T> InvokeAsync<T>(Func<T> action, CancellationToken cancellationToken = default);
}

/// <summary>A generation-aware handle that becomes invalid when the host rebuilds a target.</summary>
public interface IUiTargetHandle
{
    UiTargetId Target { get; }

    long Generation { get; }

    bool IsAlive { get; }

    ValueTask<TResult> AccessAsync<TResult>(
        Func<Control, TResult> access,
        CancellationToken cancellationToken = default);
}

/// <summary>Plugin-owned Avalonia page registered in PCL N's main navigation.</summary>
public sealed record AvaloniaPluginPageDescriptor
{
    public AvaloniaPluginPageDescriptor(PluginPageDescriptor page, Func<Control> createPage)
    {
        Page = page ?? throw new ArgumentNullException(nameof(page));
        CreatePage = createPage ?? throw new ArgumentNullException(nameof(createPage));
    }

    public PluginPageDescriptor Page { get; init; }

    public Func<Control> CreatePage { get; init; }
}

public interface IAvaloniaPluginPageService : IPluginNavigationService
{
    IPluginRegistration Register(AvaloniaPluginPageDescriptor descriptor);
}

/// <summary>Plugin-owned top-level window factory tracked by the plugin lifetime.</summary>
public sealed record AvaloniaPluginWindowDescriptor
{
    public AvaloniaPluginWindowDescriptor(string operationId, string id, Func<Window> createWindow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        OperationId = operationId.Trim();
        Id = id.Trim();
        CreateWindow = createWindow ?? throw new ArgumentNullException(nameof(createWindow));
    }

    public string OperationId { get; init; }

    public string Id { get; init; }

    public Func<Window> CreateWindow { get; init; }
}

public interface IAvaloniaPluginWindowService : IPluginService
{
    IPluginRegistration Register(AvaloniaPluginWindowDescriptor descriptor);

    ValueTask<Window> ShowAsync(string id, CancellationToken cancellationToken = default);

    IReadOnlyList<Window> ListOpenWindows();
}
