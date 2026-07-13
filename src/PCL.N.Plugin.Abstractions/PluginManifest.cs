using System.Text.Json;
using System.Text.Json.Serialization;

namespace PCL.N.Plugin;

public sealed record PluginManifest
{
    public required int FormatVersion { get; init; }

    public required int ManifestVersion { get; init; }

    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Version { get; init; }

    public string Channel { get; init; } = "stable";

    public string? Summary { get; init; }

    public string? Description { get; init; }

    public required PluginPublisherManifest Publisher { get; init; }

    public IReadOnlyList<PluginAuthorManifest> Authors { get; init; } = [];

    public required string License { get; init; }

    public string? Homepage { get; init; }

    public string? Source { get; init; }

    public string? Issues { get; init; }

    public string? Icon { get; init; }

    public required PluginEntryPointManifest EntryPoint { get; init; }

    public required PluginApiRangeManifest Api { get; init; }

    public required PluginHostRangeManifest Host { get; init; }

    public PluginPlatformManifest Platforms { get; init; } = new();

    public IReadOnlyList<PluginDependencyManifest> Dependencies { get; init; } = [];

    public IReadOnlyList<PluginIncompatibilityManifest> Incompatibilities { get; init; } = [];

    public PluginServiceRequirementsManifest Services { get; init; } = new();

    public IReadOnlyList<PluginPermissionManifest> Permissions { get; init; } = [];

    public JsonElement? Ui { get; init; }

    public PluginDataManifest Data { get; init; } = new();

    public PluginActivationManifest Activation { get; init; } = new();

    public PluginUpdateManifest Update { get; init; } = new();

    public PluginSigningManifest? Signing { get; init; }

    public IReadOnlyList<string> Categories { get; init; } = [];

    public IReadOnlyList<string> Tags { get; init; } = [];
}

public sealed record PluginPublisherManifest(string Id, string Namespace);

public sealed record PluginAuthorManifest(string Name, string? Id = null, string? Url = null);

public sealed record PluginEntryPointManifest(string Assembly, string Type);

public sealed record PluginApiRangeManifest(string Minimum, string MaximumExclusive);

public sealed record PluginHostRangeManifest(string MinimumVersion, string? MaximumVersionExclusive = null);

public sealed record PluginPlatformManifest
{
    public IReadOnlyList<string> OperatingSystems { get; init; } = [];

    public IReadOnlyList<string> Architectures { get; init; } = [];

    public IReadOnlyList<string> RuntimeIdentifiers { get; init; } = [];
}

public sealed record PluginDependencyManifest(string Id, string Version, string Kind = "required");

public sealed record PluginIncompatibilityManifest(string Id, string Version, string? Reason = null);

public sealed record PluginServiceRequirementsManifest
{
    public IReadOnlyDictionary<string, string> Required { get; init; } = new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> Optional { get; init; } = new Dictionary<string, string>();
}

public sealed record PluginPermissionManifest(string Id, string Reason);

public sealed record PluginDataManifest(int SchemaVersion = 1, int MinimumReadableSchema = 1);

public sealed record PluginActivationManifest(string Mode = "startup");

public sealed record PluginUpdateManifest(bool AllowAutomaticUpdate = true, bool RequiresRestart = false);

public sealed record PluginSigningManifest(string Fingerprint);

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    PropertyNameCaseInsensitive = false,
    ReadCommentHandling = JsonCommentHandling.Disallow,
    AllowTrailingCommas = false)]
[JsonSerializable(typeof(PluginManifest))]
public partial class PluginManifestJsonContext : JsonSerializerContext;
