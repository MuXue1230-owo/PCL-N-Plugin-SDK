namespace PCL.N.Plugin;

public enum PluginPackageAssetStatus
{
    Success,
    NotFound,
    InvalidPath,
    IntegrityFailure,
    Unavailable
}

public sealed record PluginPackageAsset(
    string RelativePath,
    string FullPath,
    long Size,
    string Sha256);

public sealed record PluginPackageAssetResult(
    PluginPackageAssetStatus Status,
    PluginPackageAsset? Asset = null,
    string? Message = null)
{
    public bool IsSuccess => Status == PluginPackageAssetStatus.Success && Asset is not null;
}

/// <summary>
/// Resolves a file from the currently installed, signed plugin package and verifies its size and
/// SHA-256 digest against the signed PNP file table before returning an absolute path.
/// </summary>
public interface IPluginPackageAssetService : IPluginService
{
    ValueTask<PluginPackageAssetResult> ResolveAsync(
        string relativePath,
        CancellationToken cancellationToken = default);
}
