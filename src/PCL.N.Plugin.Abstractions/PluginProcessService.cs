namespace PCL.N.Plugin;

public sealed record PluginProcessRequest
{
    public required string FileName { get; init; }
    public IReadOnlyList<string> Arguments { get; init; } = [];
    public string? WorkingDirectory { get; init; }
    public IReadOnlyDictionary<string, string?> EnvironmentVariables { get; init; } = new Dictionary<string, string?>();
    public bool CaptureOutput { get; init; }
    public string? StandardInput { get; init; }
    public TimeSpan? Timeout { get; init; }
}

public sealed record PluginProcessResult(int ExitCode, string StandardOutput, string StandardError);

public interface IPluginProcessService : IPluginService
{
    Task<PluginProcessResult> RunAsync(PluginProcessRequest request, CancellationToken cancellationToken = default);
}
