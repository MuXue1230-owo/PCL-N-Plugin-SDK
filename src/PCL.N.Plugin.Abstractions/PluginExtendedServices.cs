namespace PCL.N.Plugin;

public interface IPluginClipboardService : IPluginService
{
    ValueTask<string?> ReadTextAsync(CancellationToken cancellationToken = default);
    ValueTask WriteTextAsync(string text, CancellationToken cancellationToken = default);
}

public interface IPluginFileService : IPluginService
{
    ValueTask<byte[]?> ReadAsync(string relativePath, CancellationToken cancellationToken = default);
    ValueTask WriteAsync(string relativePath, ReadOnlyMemory<byte> content, CancellationToken cancellationToken = default);
    ValueTask DeleteAsync(string relativePath, CancellationToken cancellationToken = default);
    IReadOnlyList<string> List(string relativeDirectory = "");
}

public sealed record PluginAccountProviderInfo(string Id, string DisplayName, string? Description);

public interface IPluginAccountReadService : IPluginService
{
    IReadOnlyList<PluginAccountProviderInfo> ListProviders();
}

public sealed record PluginDownloadSourceInfo(string Id, string DisplayName, Uri BaseUri, string Kind);

public interface IPluginDownloadService : IPluginService
{
    IReadOnlyList<PluginDownloadSourceInfo> ListSources();
}

public sealed record PluginLaunchModification(
    string Id,
    Func<PluginLaunchRequest, PluginLaunchRequest> Apply);

public sealed record PluginLaunchRequest(
    string InstanceId,
    IReadOnlyList<string> JvmArguments,
    IReadOnlyList<string> GameArguments,
    IReadOnlyDictionary<string, string> EnvironmentVariables);

public interface IPluginLaunchModificationService : IPluginService
{
    IPluginRegistration Register(PluginLaunchModification modification);
}
