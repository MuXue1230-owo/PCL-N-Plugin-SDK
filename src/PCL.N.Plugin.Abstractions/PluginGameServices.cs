namespace PCL.N.Plugin;

public enum PluginGameSessionState
{
    Starting,
    Running,
    Exited,
    Crashed,
    Terminated
}

public enum PluginGameOutputChannel
{
    StandardOutput,
    StandardError,
    Launcher
}

public sealed record PluginGameSessionSnapshot(
    string SessionId,
    string InstanceId,
    int ProcessId,
    PluginGameSessionState State,
    DateTimeOffset StartedAt,
    DateTimeOffset? EndedAt,
    int? ExitCode,
    long LastSequence,
    string? LanAddress = null);

public sealed record PluginGameProcessOutput(
    long Sequence,
    string SessionId,
    PluginGameOutputChannel Stream,
    string Text,
    DateTimeOffset Timestamp);

public sealed record PluginLaunchEvent(
    long Sequence,
    string SessionId,
    string Kind,
    DateTimeOffset Timestamp,
    PluginGameSessionSnapshot Session);

public interface IPluginGameSessionService : IPluginService
{
    IReadOnlyList<PluginGameSessionSnapshot> ListSessions();
    bool TryGetSession(string sessionId, out PluginGameSessionSnapshot? session);
}

public interface IPluginGameOutputService : IPluginService
{
    IReadOnlyList<PluginGameProcessOutput> Read(string sessionId, long afterSequence, int maximumCount = 256);
    IPluginRegistration Subscribe(Action<PluginGameProcessOutput> observer);
}

public interface IPluginLaunchEventService : IPluginService
{
    IPluginRegistration Subscribe(Action<PluginLaunchEvent> observer);
}
