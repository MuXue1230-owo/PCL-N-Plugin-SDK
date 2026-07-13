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

    public PluginUiManifest? Ui { get; init; }

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

public sealed record PluginUiManifest
{
    public required PluginApiRangeManifest Api { get; init; }

    public PluginAvaloniaRangeManifest? Avalonia { get; init; }

    public bool RequiresRestart { get; init; }

    public IReadOnlyList<PluginUiTargetManifest> Targets { get; init; } = [];
}

public sealed record PluginAvaloniaRangeManifest(string Minimum, string MaximumExclusive);

public sealed record PluginUiTargetManifest
{
    public required string Target { get; init; }

    public required string Surface { get; init; }

    public IReadOnlyList<string> Access { get; init; } = [];

    public IReadOnlyList<PluginUiOperationManifest> Operations { get; init; } = [];

    public PluginUiCompatibilityManifest Compatibility { get; init; } = new();
}

public sealed record PluginUiOperationManifest
{
    public required string Id { get; init; }

    public required string Kind { get; init; }

    public string? Slot { get; init; }

    public string? Selector { get; init; }

    /// <summary>
    /// Safe package-relative path to a declarative AXAML resource, normally under <c>ui/</c>.
    /// Code-behind and launcher-private CLR namespaces are not part of this contract.
    /// </summary>
    public string? Axaml { get; init; }

    /// <summary>Optional public command ID used by bindings declared in the AXAML resource.</summary>
    public string? Command { get; init; }

    public int Priority { get; init; }

    public bool Required { get; init; }

    public string Fallback { get; init; } = "skip-patch";

    public IReadOnlyList<string> Before { get; init; } = [];

    public IReadOnlyList<string> After { get; init; } = [];

    public string? PropertyPath { get; init; }

    public string? ModifyPolicy { get; init; }

    public bool AllowWrapping { get; init; }

    public PluginUiPreconditionsManifest? Preconditions { get; init; }
}

public sealed record PluginUiPreconditionsManifest
{
    public string? Surface { get; init; }

    public IReadOnlyList<string> RequiredSlots { get; init; } = [];

    public IReadOnlyList<string> RequiredProperties { get; init; } = [];

    public IReadOnlyList<string> RequiredEvents { get; init; } = [];
}

public sealed record PluginUiCompatibilityManifest
{
    public IReadOnlyDictionary<string, string> CompatibleWith { get; init; } =
        new Dictionary<string, string>();

    public IReadOnlyDictionary<string, string> IncompatibleWith { get; init; } =
        new Dictionary<string, string>();
}

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
