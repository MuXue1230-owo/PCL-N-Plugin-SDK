namespace PCL.N.Plugin;

/// <summary>
/// Remote plugin marketplace client contract (design §19.2).
/// Hosts may implement this contract against the public PCL.N plugin catalog.
/// </summary>
/// <remarks>
/// Production HTTP surface:
/// <list type="bullet">
/// <item><c>GET /v1/plugins</c></item>
/// <item><c>GET /v1/plugins/{pluginId}</c></item>
/// <item><c>GET /v1/plugins/{pluginId}/versions</c></item>
/// <item><c>GET /v1/plugins/{pluginId}/versions/{version}</c></item>
/// <item><c>GET /v1/plugins/{pluginId}/versions/{version}/download</c></item>
/// <item><c>GET /v1/categories</c></item>
/// <item><c>GET /v1/publishers/{publisherId}</c></item>
/// </list>
/// Local folder scanning is host-only and is not part of this remote client ABI.
/// </remarks>
public interface IPluginMarketClient
{
    /// <summary>Whether a remote market endpoint is configured and reachable in principle.</summary>
    bool IsRemoteConfigured { get; }

    ValueTask<IReadOnlyList<PluginMarketPluginSummary>> ListPluginsAsync(
        PluginMarketListQuery? query = null,
        CancellationToken cancellationToken = default);

    ValueTask<PluginMarketPluginDetail?> GetPluginAsync(
        string pluginId,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<PluginMarketVersionInfo>> ListVersionsAsync(
        string pluginId,
        CancellationToken cancellationToken = default);

    ValueTask<PluginMarketVersionInfo?> GetVersionAsync(
        string pluginId,
        string version,
        CancellationToken cancellationToken = default);

    /// <summary>Resolves a download URL or artifact locator for a published version.</summary>
    ValueTask<PluginMarketDownloadInfo?> GetDownloadAsync(
        string pluginId,
        string version,
        CancellationToken cancellationToken = default);

    ValueTask<IReadOnlyList<PluginMarketCategory>> ListCategoriesAsync(
        CancellationToken cancellationToken = default);

    ValueTask<PluginMarketPublisher?> GetPublisherAsync(
        string publisherId,
        CancellationToken cancellationToken = default);
}

public sealed record PluginMarketListQuery(
    string? Search = null,
    string? Category = null,
    string? PublisherId = null,
    int Skip = 0,
    int Take = 50);

public sealed record PluginMarketPluginSummary(
    string PluginId,
    string Name,
    string? Summary,
    string? LatestVersion,
    string? PublisherId,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    string? Category = null,
    string PricingModel = "free",
    int PriceCents = 0,
    string Currency = "CNY",
    bool RequiresPurchase = false);

public sealed record PluginMarketPluginDetail(
    string PluginId,
    string Name,
    string? Summary,
    string? Description,
    string? Homepage,
    string? Source,
    string? License,
    string? PublisherId,
    string? PublisherName,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> UiTargets,
    string? LatestVersion,
    string? Category = null,
    string PricingModel = "free",
    int PriceCents = 0,
    string Currency = "CNY",
    bool RequiresPurchase = false);

public sealed record PluginMarketVersionInfo(
    string PluginId,
    string Version,
    string Channel,
    DateTimeOffset? PublishedAt,
    string? Changelog,
    string? PackageSha256,
    string? SignatureFingerprint,
    long? PackageSizeBytes,
    IReadOnlyDictionary<string, string> RequiredServices,
    IReadOnlyList<PluginMarketDependency> Dependencies);

public sealed record PluginMarketDependency(
    string PluginId,
    string VersionRange,
    string Kind);

public sealed record PluginMarketDownloadInfo(
    string PluginId,
    string Version,
    Uri DownloadUri,
    string? PackageSha256,
    IReadOnlyDictionary<string, string> Headers);

public sealed record PluginMarketCategory(
    string Id,
    string Name,
    string? Description);

public sealed record PluginMarketPublisher(
    string Id,
    string Name,
    string? Homepage,
    string? Namespace);

public enum PluginMarketAccessFailure
{
    Unknown = 0,
    AuthenticationRequired = 1,
    PurchaseRequired = 2,
    NotFound = 3
}

public sealed class PluginMarketAccessException(
    PluginMarketAccessFailure failure,
    string message) : InvalidOperationException(message)
{
    public PluginMarketAccessFailure Failure { get; } = failure;
}

/// <summary>
/// Optional fallback client for hosts that deliberately disable online market access.
/// All operations report "not configured".
/// </summary>
public sealed class UnconfiguredPluginMarketClient : IPluginMarketClient
{
    public static UnconfiguredPluginMarketClient Instance { get; } = new();

    public bool IsRemoteConfigured => false;

    public ValueTask<IReadOnlyList<PluginMarketPluginSummary>> ListPluginsAsync(
        PluginMarketListQuery? query = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<PluginMarketPluginSummary>>([]);
    }

    public ValueTask<PluginMarketPluginDetail?> GetPluginAsync(
        string pluginId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<PluginMarketPluginDetail?>(null);
    }

    public ValueTask<IReadOnlyList<PluginMarketVersionInfo>> ListVersionsAsync(
        string pluginId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<PluginMarketVersionInfo>>([]);
    }

    public ValueTask<PluginMarketVersionInfo?> GetVersionAsync(
        string pluginId,
        string version,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<PluginMarketVersionInfo?>(null);
    }

    public ValueTask<PluginMarketDownloadInfo?> GetDownloadAsync(
        string pluginId,
        string version,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<PluginMarketDownloadInfo?>(null);
    }

    public ValueTask<IReadOnlyList<PluginMarketCategory>> ListCategoriesAsync(
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<IReadOnlyList<PluginMarketCategory>>([]);
    }

    public ValueTask<PluginMarketPublisher?> GetPublisherAsync(
        string publisherId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return ValueTask.FromResult<PluginMarketPublisher?>(null);
    }
}
