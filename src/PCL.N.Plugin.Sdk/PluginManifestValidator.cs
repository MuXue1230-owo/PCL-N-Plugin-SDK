using System.Text.Json;

namespace PCL.N.Plugin.Sdk;

public sealed record PluginManifestValidationIssue(string Code, string Path, string Message);

public sealed record PluginManifestValidationResult(
    PluginManifest? Manifest,
    IReadOnlyList<PluginManifestValidationIssue> Issues)
{
    public bool IsValid => Manifest is not null && Issues.Count == 0;
}

public static class PluginManifestValidator
{
    private static readonly HashSet<string> Channels = new(StringComparer.Ordinal)
    {
        "stable", "beta", "alpha", "nightly"
    };

    private static readonly HashSet<string> OperatingSystems = new(StringComparer.Ordinal)
    {
        "windows", "linux", "macos"
    };

    private static readonly HashSet<string> Architectures = new(StringComparer.Ordinal)
    {
        "x64", "arm64"
    };

    public static PluginManifestValidationResult ParseAndValidate(ReadOnlySpan<byte> json)
    {
        try
        {
            PluginManifest? manifest = JsonSerializer.Deserialize(json, PluginManifestJsonContext.Default.PluginManifest);
            return manifest is null
                ? Invalid("PNPMAN001", "$", "Manifest is empty.")
                : Validate(manifest);
        }
        catch (JsonException exception)
        {
            return Invalid("PNPMAN001", exception.Path ?? "$", exception.Message);
        }
    }

    public static PluginManifestValidationResult Validate(PluginManifest manifest)
    {
        ArgumentNullException.ThrowIfNull(manifest);
        List<PluginManifestValidationIssue> issues = [];
        AddIf(manifest.FormatVersion != 1, "PNPMAN002", "$.formatVersion", "Only package format version 1 is supported.");
        AddIf(manifest.ManifestVersion != 1, "PNPMAN003", "$.manifestVersion", "Only manifest version 1 is supported.");
        AddIf(!PluginId.TryParse(manifest.Id, out PluginId pluginId), "PNPMAN004", "$.id", "Plugin ID is invalid.");
        AddIf(IsReservedId(manifest.Id), "PNPMAN005", "$.id", "Plugin ID uses a reserved namespace.");
        AddIf(string.IsNullOrWhiteSpace(manifest.Name), "PNPMAN006", "$.name", "Plugin name is required.");
        AddIf(!PluginVersion.TryParse(manifest.Version, out _), "PNPMAN007", "$.version", "Plugin version must use SemVer 2.0.");
        AddIf(!Channels.Contains(manifest.Channel), "PNPMAN008", "$.channel", "Plugin channel is not supported.");
        AddIf(string.IsNullOrWhiteSpace(manifest.License), "PNPMAN009", "$.license", "SPDX license expression is required.");
        AddIf(!IsSafeRelativePath(manifest.EntryPoint.Assembly) ||
              !manifest.EntryPoint.Assembly.StartsWith("lib/net10.0/", StringComparison.Ordinal) ||
              !manifest.EntryPoint.Assembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase),
            "PNPMAN010", "$.entryPoint.assembly", "Entry assembly must be a safe lib/net10.0 DLL path.");
        AddIf(string.IsNullOrWhiteSpace(manifest.EntryPoint.Type) || !manifest.EntryPoint.Type.Contains('.', StringComparison.Ordinal),
            "PNPMAN011", "$.entryPoint.type", "Entry type must be a fully-qualified public type name.");
        ValidateApiRange(manifest.Api, issues);
        AddIf(!PluginVersion.TryParse(manifest.Host.MinimumVersion, out PluginVersion hostMinimum),
            "PNPMAN014", "$.host.minimumVersion", "Minimum host version must use SemVer 2.0.");
        if (manifest.Host.MaximumVersionExclusive is { } maximumHost)
        {
            AddIf(!PluginVersion.TryParse(maximumHost, out PluginVersion parsedMaximum),
                "PNPMAN015", "$.host.maximumVersionExclusive", "Maximum host version must use SemVer 2.0.");
            if (PluginVersion.TryParse(manifest.Host.MinimumVersion, out hostMinimum) &&
                PluginVersion.TryParse(maximumHost, out parsedMaximum))
            {
                AddIf(hostMinimum >= parsedMaximum, "PNPMAN016", "$.host", "Host version range is empty.");
            }
        }

        PluginPublisherManifest publisher = manifest.Publisher ?? new PluginPublisherManifest(string.Empty, string.Empty);
        PluginSigningManifest signing = manifest.Signing ?? new PluginSigningManifest(string.Empty);
        AddIf(!PluginId.TryParse(publisher.Namespace, out _) ||
              !manifest.Id.StartsWith(publisher.Namespace + ".", StringComparison.Ordinal),
            "PNPMAN017", "$.publisher.namespace", "Publisher namespace must be a valid prefix of the plugin ID.");
        AddIf(string.IsNullOrWhiteSpace(publisher.Id), "PNPMAN018", "$.publisher.id", "Publisher ID is required.");
        AddIf(!IsFullFingerprint(signing.Fingerprint), "PNPMAN019", "$.signing.fingerprint", "A full hexadecimal OpenPGP fingerprint is required.");
        ValidateSigning(signing, manifest.SigningPolicy ?? new PluginSigningPolicyManifest(), issues);
        AddIf(manifest.Icon is not null && !IsSafeRelativePath(manifest.Icon), "PNPMAN020", "$.icon", "Icon path is unsafe.");

        ValidateUniqueDependencies(manifest, pluginId, issues);
        ValidatePermissions(manifest.Permissions ?? [], issues);
        ValidatePlatforms(manifest.Platforms ?? new PluginPlatformManifest(), issues);
        ValidateServiceRanges(manifest.Services ?? new PluginServiceRequirementsManifest(), issues);
        ValidateUi(manifest.Ui, issues);
        PluginDataManifest data = manifest.Data ?? new PluginDataManifest();
        AddIf(data.MinimumReadableSchema > data.SchemaVersion,
            "PNPMAN032", "$.data", "minimumReadableSchema cannot exceed schemaVersion.");
        ValidateDataMigrations(data, issues);
        ValidateNative(manifest.Native ?? new PluginNativeManifest(), issues);

        return new PluginManifestValidationResult(manifest, issues);

        void AddIf(bool condition, string code, string path, string message)
        {
            if (condition)
                issues.Add(new PluginManifestValidationIssue(code, path, message));
        }
    }

    public static bool IsSafeRelativePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path) || path.StartsWith('/') || path.StartsWith('\\') ||
            path.Contains('\\') || path.Contains(':'))
        {
            return false;
        }

        return path.Split('/').All(segment => segment.Length > 0 && segment is not "." and not "..");
    }

    private static void ValidateApiRange(PluginApiRangeManifest? api, List<PluginManifestValidationIssue> issues)
    {
        // PNPSDK008: API version range is required.
        if (api is null ||
            string.IsNullOrWhiteSpace(api.Minimum) ||
            string.IsNullOrWhiteSpace(api.MaximumExclusive))
        {
            issues.Add(new PluginManifestValidationIssue(
                "PNPSDK008",
                "$.api",
                "Plugin API version range (minimum / maximumExclusive) is required."));
            return;
        }

        bool minimumValid = TryParseApiVersion(api.Minimum, out PluginApiVersion minimum);
        bool maximumValid = TryParseApiVersion(api.MaximumExclusive, out PluginApiVersion maximum);
        if (!minimumValid)
            issues.Add(new PluginManifestValidationIssue("PNPMAN012", "$.api.minimum", "Minimum API version is invalid."));
        if (!maximumValid)
            issues.Add(new PluginManifestValidationIssue("PNPMAN013", "$.api.maximumExclusive", "Maximum API version is invalid."));
        if (minimumValid && maximumValid && minimum.CompareTo(maximum) >= 0)
            issues.Add(new PluginManifestValidationIssue("PNPMAN013", "$.api", "Plugin API version range is empty."));
    }

    private static void ValidateUniqueDependencies(
        PluginManifest manifest,
        PluginId pluginId,
        List<PluginManifestValidationIssue> issues)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        IReadOnlyList<PluginDependencyManifest> dependencies = manifest.Dependencies ?? [];
        for (int index = 0; index < dependencies.Count; index++)
        {
            PluginDependencyManifest dependency = dependencies[index];
            string path = $"$.dependencies[{index}]";
            if (!PluginId.TryParse(dependency.Id, out PluginId dependencyId))
                issues.Add(new PluginManifestValidationIssue("PNPMAN021", path + ".id", "Dependency ID is invalid."));
            else if (dependencyId == pluginId)
                issues.Add(new PluginManifestValidationIssue("PNPMAN022", path + ".id", "A plugin cannot depend on itself."));
            if (!ids.Add(dependency.Id))
                issues.Add(new PluginManifestValidationIssue("PNPMAN023", path + ".id", "Dependency ID is duplicated."));
            if (dependency.Kind is not "required" and not "optional")
                issues.Add(new PluginManifestValidationIssue("PNPMAN024", path + ".kind", "Dependency kind must be required or optional."));
            if (!PluginVersionRange.TryParse(dependency.Version, out _))
                issues.Add(new PluginManifestValidationIssue("PNPMAN025", path + ".version", "Dependency version range is invalid."));
        }
    }

    private static void ValidatePermissions(
        IReadOnlyList<PluginPermissionManifest> permissions,
        List<PluginManifestValidationIssue> issues)
    {
        HashSet<string> ids = new(StringComparer.Ordinal);
        for (int index = 0; index < permissions.Count; index++)
        {
            PluginPermissionManifest permission = permissions[index];
            string path = $"$.permissions[{index}]";
            if (string.IsNullOrWhiteSpace(permission.Id) || !ids.Add(permission.Id))
                issues.Add(new PluginManifestValidationIssue("PNPMAN026", path + ".id", "Permission ID is empty or duplicated."));
            if (string.IsNullOrWhiteSpace(permission.Reason))
                issues.Add(new PluginManifestValidationIssue("PNPMAN027", path + ".reason", "Permission reason is required."));
            string kind = string.IsNullOrWhiteSpace(permission.Kind) ? "required" : permission.Kind.Trim();
            if (kind is not "required" and not "optional")
                issues.Add(new PluginManifestValidationIssue("PNPMAN027b", path + ".kind", "Permission kind must be required or optional."));
        }
    }

    private static void ValidatePlatforms(
        PluginPlatformManifest platforms,
        List<PluginManifestValidationIssue> issues)
    {
        if (platforms.OperatingSystems.Any(os => !OperatingSystems.Contains(os)))
            issues.Add(new PluginManifestValidationIssue("PNPMAN028", "$.platforms.operatingSystems", "Unsupported operating system."));
        if (platforms.Architectures.Any(architecture => !Architectures.Contains(architecture)))
            issues.Add(new PluginManifestValidationIssue("PNPMAN029", "$.platforms.architectures", "Unsupported architecture."));
    }

    private static void ValidateServiceRanges(
        PluginServiceRequirementsManifest services,
        List<PluginManifestValidationIssue> issues)
    {
        IReadOnlyDictionary<string, string> required = services.Required ?? new Dictionary<string, string>();
        IReadOnlyDictionary<string, string> optional = services.Optional ?? new Dictionary<string, string>();
        foreach ((string id, string range) in required.Concat(optional))
        {
            if (string.IsNullOrWhiteSpace(id) || !PluginVersionRange.TryParse(NormalizeServiceRange(range), out _))
                issues.Add(new PluginManifestValidationIssue("PNPMAN030", "$.services", $"Service requirement '{id}' has an invalid range."));
        }
        if (required.Keys.Intersect(optional.Keys, StringComparer.Ordinal).Any())
            issues.Add(new PluginManifestValidationIssue("PNPMAN031", "$.services", "A service cannot be both required and optional."));
    }

    private static void ValidateUi(PluginUiManifest? ui, List<PluginManifestValidationIssue> issues)
    {
        if (ui is null)
            return;

        ValidateApiRange(ui.Api, issues);
        HashSet<string> targets = new(StringComparer.Ordinal);
        HashSet<string> operationIds = new(StringComparer.Ordinal);
        for (int targetIndex = 0; targetIndex < ui.Targets.Count; targetIndex++)
        {
            PluginUiTargetManifest target = ui.Targets[targetIndex];
            string targetPath = $"$.ui.targets[{targetIndex}]";
            if (string.IsNullOrWhiteSpace(target.Target) || !targets.Add(target.Target))
                issues.Add(new PluginManifestValidationIssue("PNPMAN033", targetPath + ".target", "UI target is empty or duplicated."));
            if (!PluginVersionRange.TryParse(NormalizeServiceRange(target.Surface), out _))
                issues.Add(new PluginManifestValidationIssue("PNPMAN034", targetPath + ".surface", "UI surface version range is invalid."));

            HashSet<string> access = new(StringComparer.Ordinal);
            foreach (string value in target.Access)
            {
                if (!access.Add(value) || value is not ("register" or "observe" or "inject" or "modify" or "replace" or "wrap" or "resources" or "global" or "raw-access" or "input-intercept" or "window-management"))
                    issues.Add(new PluginManifestValidationIssue("PNPMAN035", targetPath + ".access", $"UI access '{value}' is invalid or duplicated."));
            }

            for (int operationIndex = 0; operationIndex < target.Operations.Count; operationIndex++)
            {
                PluginUiOperationManifest operation = target.Operations[operationIndex];
                string operationPath = $"{targetPath}.operations[{operationIndex}]";
                if (string.IsNullOrWhiteSpace(operation.Id) || !operationIds.Add(operation.Id))
                    issues.Add(new PluginManifestValidationIssue("PNPMAN036", operationPath + ".id", "UI operation ID is empty or duplicated."));
                if (operation.Kind is not ("observe" or "register" or "inject" or "modify" or "replace" or "remove" or "reorder" or "override-resource" or "override-style" or "override-template" or "intercept-input" or "wrap"))
                    issues.Add(new PluginManifestValidationIssue("PNPMAN037", operationPath + ".kind", "UI operation kind is unsupported."));
                if (operation.Kind == "inject" && string.IsNullOrWhiteSpace(operation.Slot))
                    issues.Add(new PluginManifestValidationIssue("PNPMAN038", operationPath + ".slot", "Inject operations require a slot."));
                if (operation.Axaml is not null &&
                    (!IsSafeRelativePath(operation.Axaml) ||
                     !operation.Axaml.StartsWith("ui/", StringComparison.Ordinal) ||
                     !operation.Axaml.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase)))
                {
                    issues.Add(new PluginManifestValidationIssue("PNPMAN040", operationPath + ".axaml", "AXAML must be a safe package path under ui/ ending in .axaml."));
                }
                if (operation.Axaml is not null && operation.Kind is not ("register" or "inject" or "replace" or "wrap"))
                    issues.Add(new PluginManifestValidationIssue("PNPMAN041", operationPath + ".axaml", "AXAML is only supported by register, inject, replace, or wrap operations."));
                if (operation.Kind == "modify" && string.IsNullOrWhiteSpace(operation.Selector) && string.IsNullOrWhiteSpace(operation.PropertyPath))
                    issues.Add(new PluginManifestValidationIssue("PNPMAN039", operationPath, "Modify operations require a selector or propertyPath."));
                string requiredAccess = operation.Kind switch
                {
                    "register" => "register",
                    "observe" => "observe",
                    "inject" or "reorder" => "inject",
                    "modify" or "wrap" => "modify",
                    "replace" or "remove" => "replace",
                    "override-resource" or "override-style" or "override-template" => "resources",
                    "intercept-input" => "input-intercept",
                    _ => string.Empty
                };
                if (requiredAccess.Length > 0 && !access.Contains(requiredAccess))
                    issues.Add(new PluginManifestValidationIssue("PNPMAN042", operationPath + ".kind", $"Operation requires target access '{requiredAccess}'."));
            }

            ValidateUiCompatibility(target.Compatibility, targetPath + ".compatibility", issues);
        }
    }

    private static void ValidateUiCompatibility(
        PluginUiCompatibilityManifest? compatibility,
        string path,
        List<PluginManifestValidationIssue> issues)
    {
        if (compatibility is null)
            return;
        foreach ((string pluginId, string range) in (compatibility.CompatibleWith ?? new Dictionary<string, string>())
                     .Concat(compatibility.IncompatibleWith ?? new Dictionary<string, string>()))
        {
            if (pluginId == "*" || !PluginId.TryParse(pluginId, out _) ||
                !PluginVersionRange.TryParse(range, out _))
            {
                issues.Add(new PluginManifestValidationIssue(
                    "PNPMAN043",
                    path,
                    $"UI compatibility entry '{pluginId}' must name a concrete plugin and valid version range."));
            }
        }
    }

    private static void ValidateDataMigrations(
        PluginDataManifest data,
        List<PluginManifestValidationIssue> issues)
    {
        IReadOnlyList<PluginDataMigrationManifest> migrations = data.Migrations ?? [];
        HashSet<string> ids = new(StringComparer.Ordinal);
        Dictionary<int, int> transitions = [];
        for (int index = 0; index < migrations.Count; index++)
        {
            PluginDataMigrationManifest migration = migrations[index];
            string path = $"$.data.migrations[{index}]";
            if (string.IsNullOrWhiteSpace(migration.Id) || !ids.Add(migration.Id))
                issues.Add(new PluginManifestValidationIssue("PNPMAN044", path + ".id", "Migration ID is empty or duplicated."));
            if (migration.From < data.MinimumReadableSchema || migration.To > data.SchemaVersion || migration.From >= migration.To)
                issues.Add(new PluginManifestValidationIssue("PNPMAN045", path, "Migration transition is outside the readable schema range or does not advance."));
            if (!transitions.TryAdd(migration.From, migration.To))
                issues.Add(new PluginManifestValidationIssue("PNPMAN046", path + ".from", "Only one migration may start from a schema version."));
        }

        if (data.MinimumReadableSchema < data.SchemaVersion)
        {
            int current = data.MinimumReadableSchema;
            HashSet<int> visited = [];
            while (current < data.SchemaVersion && visited.Add(current) && transitions.TryGetValue(current, out int next))
                current = next;
            if (current != data.SchemaVersion)
                issues.Add(new PluginManifestValidationIssue("PNPMAN047", "$.data.migrations", "Migrations must provide a complete path from minimumReadableSchema to schemaVersion."));
        }
    }

    private static void ValidateNative(
        PluginNativeManifest native,
        List<PluginManifestValidationIssue> issues)
    {
        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<PluginNativeLibraryManifest> libraries = native.Libraries ?? [];
        for (int index = 0; index < libraries.Count; index++)
        {
            string name = libraries[index].Name;
            if (string.IsNullOrWhiteSpace(name) || name.Contains('/') || name.Contains('\\') || !names.Add(name))
                issues.Add(new PluginManifestValidationIssue("PNPMAN048", $"$.native.libraries[{index}].name", "Native library name is empty, unsafe, or duplicated."));
        }
        if (libraries.Any(static library => !library.Unloadable) && !native.RequiresRestartForUpdate)
            issues.Add(new PluginManifestValidationIssue("PNPMAN049", "$.native.requiresRestartForUpdate", "Non-unloadable native libraries require restart for update."));
    }

    private static void ValidateSigning(
        PluginSigningManifest signing,
        PluginSigningPolicyManifest policy,
        List<PluginManifestValidationIssue> issues)
    {
        HashSet<string> fingerprints = new(StringComparer.OrdinalIgnoreCase);
        if (IsFullFingerprint(signing.Fingerprint))
            fingerprints.Add(signing.Fingerprint);
        IReadOnlyList<string> additional = signing.Fingerprints ?? [];
        for (int index = 0; index < additional.Count; index++)
        {
            string fingerprint = additional[index];
            if (!IsFullFingerprint(fingerprint))
                issues.Add(new PluginManifestValidationIssue("PNPMAN053", $"$.signing.fingerprints[{index}]", "A full hexadecimal OpenPGP fingerprint is required."));
            else if (!fingerprints.Add(fingerprint))
                issues.Add(new PluginManifestValidationIssue("PNPMAN054", $"$.signing.fingerprints[{index}]", "Signing fingerprints must be unique."));
        }
        IReadOnlyList<string> revoked = signing.RevokedFingerprints ?? [];
        for (int index = 0; index < revoked.Count; index++)
        {
            string fingerprint = revoked[index];
            if (!IsFullFingerprint(fingerprint))
                issues.Add(new PluginManifestValidationIssue("PNPMAN055", $"$.signing.revokedFingerprints[{index}]", "A full hexadecimal OpenPGP fingerprint is required."));
            else if (fingerprints.Contains(fingerprint))
                issues.Add(new PluginManifestValidationIssue("PNPMAN056", $"$.signing.revokedFingerprints[{index}]", "A revoked fingerprint cannot be used for package signing."));
        }

        if (policy.MinimumValidSignatures < 1)
            issues.Add(new PluginManifestValidationIssue("PNPMAN050", "$.signingPolicy.minimumValidSignatures", "At least one valid signature is required."));
        IReadOnlyList<string> roles = policy.Roles ?? [];
        if (roles.Any(string.IsNullOrWhiteSpace) || roles.Distinct(StringComparer.Ordinal).Count() != roles.Count)
            issues.Add(new PluginManifestValidationIssue("PNPMAN051", "$.signingPolicy.roles", "Signing roles must be non-empty and unique."));
        if (policy.MinimumValidSignatures > Math.Max(1, fingerprints.Count))
            issues.Add(new PluginManifestValidationIssue("PNPMAN052", "$.signingPolicy", "minimumValidSignatures cannot exceed the declared signing key count."));
    }

    private static string NormalizeServiceRange(string range) =>
        string.Join(' ', range.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(token =>
        {
            int dots = token.Count(character => character == '.');
            return dots == 1 && token != "*" ? token + ".0" : token;
        }));

    private static bool TryParseApiVersion(string? value, out PluginApiVersion version)
    {
        version = default;
        string[] parts = value?.Split('.') ?? [];
        if (parts.Length != 2 || !int.TryParse(parts[0], out int major) || !int.TryParse(parts[1], out int minor) || major < 0 || minor < 0)
            return false;
        version = new PluginApiVersion(major, minor);
        return true;
    }

    private static bool IsReservedId(string id) =>
        id.StartsWith("pcl.", StringComparison.Ordinal) ||
        id.StartsWith("pcl-n.", StringComparison.Ordinal) ||
        id.StartsWith("official.", StringComparison.Ordinal) ||
        id.StartsWith("system.", StringComparison.Ordinal) ||
        id.StartsWith("internal.", StringComparison.Ordinal);

    private static bool IsFullFingerprint(string? value) =>
        !string.IsNullOrWhiteSpace(value) && value.Length >= 40 && value.All(Uri.IsHexDigit);

    private static PluginManifestValidationResult Invalid(string code, string path, string message) =>
        new(null, [new PluginManifestValidationIssue(code, path, message)]);
}
