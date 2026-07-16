using System.IO.Compression;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

return await PnpPackCommand.RunAsync(args).ConfigureAwait(false);

internal static class PnpPackCommand
{
    private static readonly DateTimeOffset ZipTimestamp = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly HashSet<string> ForbiddenAssemblies = new(StringComparer.OrdinalIgnoreCase)
    {
        "PCL.N.Plugin.Abstractions.dll", "PCL.N.Plugin.UI.dll", "PCL.N.Plugin.UI.Avalonia.dll",
        "PCL.Application.dll", "PCL.Desktop.dll", "PCL.Plugin.dll"
    };

    public static async Task<int> RunAsync(string[] args)
    {
        try
        {
            await PackAsync(PackOptions.Parse(args)).ConfigureAwait(false);
            return 0;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("PNPBUILD001: " + exception.Message);
            return 1;
        }
    }

    private static async Task PackAsync(PackOptions options)
    {
        byte[] manifestSource = await File.ReadAllBytesAsync(options.ManifestPath).ConfigureAwait(false);
        string? developmentGpgHome = null;
        string? developmentFingerprint = null;
        if (!options.Sign)
        {
            developmentGpgHome = options.DevelopmentGpgHome ?? GetDefaultDevelopmentGpgHome();
            developmentFingerprint = await GpgSigner.GetOrCreateDevelopmentFingerprintAsync(
                options.GpgPath,
                developmentGpgHome).ConfigureAwait(false);
            JsonObject manifestObject = JsonNode.Parse(manifestSource)?.AsObject()
                ?? throw new InvalidOperationException("Manifest root must be a JSON object.");
            manifestObject["signing"] = new JsonObject { ["fingerprint"] = developmentFingerprint };
            manifestSource = JsonSerializer.SerializeToUtf8Bytes(manifestObject);
        }

        byte[] manifest = CanonicalJson.Normalize(manifestSource);
        using JsonDocument document = JsonDocument.Parse(manifest);
        JsonElement root = document.RootElement;
        string pluginId = RequiredString(root, "id");
        string pluginVersion = RequiredString(root, "version");
        string entryPath = RequiredString(root.GetProperty("entryPoint"), "assembly");
        string[] fingerprints = root.TryGetProperty("signing", out JsonElement signing) && signing.ValueKind == JsonValueKind.Object
            ? ReadSigningFingerprints(signing)
            : [];
        string? fingerprint = fingerprints.FirstOrDefault();

        Dictionary<string, byte[]> files = new(StringComparer.Ordinal) { ["plugin.json"] = manifest };
        AddFile(files, entryPath, options.AssemblyPath);
        if (options.DepsPath is not null && File.Exists(options.DepsPath))
            AddFile(files, Path.ChangeExtension(entryPath, ".deps.json").Replace('\\', '/'), options.DepsPath);
        AddManifestAxaml(files, root, options.ContentRoot);
        foreach (string dependency in options.Dependencies)
            AddFile(files, "lib/net10.0/" + Path.GetFileName(dependency), dependency);
        foreach (string native in options.NativeFiles)
        {
            int separator = native.IndexOf('|');
            if (separator <= 0 || separator == native.Length - 1)
                throw new InvalidOperationException("Native assets require RuntimeIdentifier metadata.");
            string rid = native[..separator];
            string path = native[(separator + 1)..];
            AddFile(files, $"runtimes/{rid}/native/{Path.GetFileName(path)}", path);
        }
        foreach (string path in options.ContentFiles)
            AddFile(files, NormalizePath(Path.GetRelativePath(options.ContentRoot, Path.GetFullPath(path))), path);
        Validate(files);

        FileRecord[] records = files.OrderBy(static item => item.Key, Utf8PathComparer.Instance)
            .Select(static item => new FileRecord(item.Key, item.Value.LongLength, Hash(item.Value))).ToArray();
        byte[] table = CanonicalJson.Serialize(new
        {
            formatVersion = 1,
            hashAlgorithm = "SHA-256",
            files = records.Select(static item => new { path = item.Path, size = item.Size, sha256 = item.Sha256 }).ToArray()
        });
        string payloadRoot = ComputePayloadRoot(records);
        byte[] signed = CanonicalJson.Serialize(new
        {
            signatureFormat = 1,
            packageFormat = 1,
            pluginId,
            pluginVersion,
            manifestSha256 = Hash(manifest),
            fileTableSha256 = Hash(table),
            payloadRootSha256 = payloadRoot,
            signingKeyFingerprint = fingerprint ?? string.Empty,
            signingKeyFingerprints = fingerprints
        });
        files["META-INF/pnp.files.json"] = table;
        files["META-INF/pnp.signed.json"] = signed;

        if (fingerprints.Length == 0)
            throw new InvalidOperationException("A full OpenPGP fingerprint is required for every .pnp package.");
        foreach (string keyFingerprint in fingerprints)
        {
            if (keyFingerprint.Length < 40 || keyFingerprint.Any(static value => !Uri.IsHexDigit(value)))
                throw new InvalidOperationException("A full OpenPGP fingerprint is required for every .pnp package.");
            await GpgSigner.SignAsync(
                files,
                signed,
                keyFingerprint,
                options.GpgPath,
                developmentGpgHome).ConfigureAwait(false);
        }
        if (!options.Sign)
            Console.Error.WriteLine($"PNPBUILD002: Package signed with local development key {developmentFingerprint}; official market distribution is not allowed.");

        Directory.CreateDirectory(Path.GetDirectoryName(options.OutputPath)!);
        string temporary = options.OutputPath + ".tmp";
        if (File.Exists(temporary)) File.Delete(temporary);
        using (FileStream stream = File.Create(temporary))
        using (ZipArchive archive = new(stream, ZipArchiveMode.Create, false, Encoding.UTF8))
        {
            foreach ((string path, byte[] content) in files.OrderBy(static item => item.Key, Utf8PathComparer.Instance))
            {
                ZipArchiveEntry entry = archive.CreateEntry(path, CompressionLevel.Optimal);
                entry.LastWriteTime = ZipTimestamp;
                entry.ExternalAttributes = 0;
                using Stream destination = entry.Open();
                await destination.WriteAsync(content).ConfigureAwait(false);
            }
        }
        File.Move(temporary, options.OutputPath, true);
        Console.WriteLine(options.OutputPath);
    }

    private static string[] ReadSigningFingerprints(JsonElement signing)
    {
        string primary = RequiredString(signing, "fingerprint").ToUpperInvariant();
        SortedSet<string> fingerprints = new(StringComparer.Ordinal) { primary };
        if (signing.TryGetProperty("fingerprints", out JsonElement additional) && additional.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in additional.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(item.GetString()))
                    throw new InvalidOperationException("signing.fingerprints must contain full OpenPGP fingerprints.");
                fingerprints.Add(item.GetString()!.ToUpperInvariant());
            }
        }
        if (signing.TryGetProperty("revokedFingerprints", out JsonElement revoked) && revoked.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in revoked.EnumerateArray())
            {
                if (item.ValueKind == JsonValueKind.String && fingerprints.Contains(item.GetString()!.ToUpperInvariant()))
                    throw new InvalidOperationException("A revoked signing fingerprint cannot be used to sign a package.");
            }
        }
        return fingerprints.ToArray();
    }

    private static void AddManifestAxaml(IDictionary<string, byte[]> files, JsonElement root, string contentRoot)
    {
        if (!root.TryGetProperty("ui", out JsonElement ui) || ui.ValueKind != JsonValueKind.Object ||
            !ui.TryGetProperty("targets", out JsonElement targets) || targets.ValueKind != JsonValueKind.Array)
            return;

        foreach (JsonElement target in targets.EnumerateArray())
        {
            if (!target.TryGetProperty("operations", out JsonElement operations) || operations.ValueKind != JsonValueKind.Array)
                continue;
            foreach (JsonElement operation in operations.EnumerateArray())
            {
                if (!operation.TryGetProperty("axaml", out JsonElement axaml) || axaml.ValueKind != JsonValueKind.String)
                    continue;
                string packagePath = NormalizePath(axaml.GetString()!);
                string sourcePath = Path.GetFullPath(Path.Combine(contentRoot, packagePath.Replace('/', Path.DirectorySeparatorChar)));
                string rootPath = Path.GetFullPath(contentRoot) + Path.DirectorySeparatorChar;
                if (!sourcePath.StartsWith(rootPath, OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal))
                    throw new InvalidOperationException($"AXAML escapes the content root: {packagePath}");
                string source = File.ReadAllText(sourcePath);
                if (source.Contains("x:Class=", StringComparison.Ordinal) ||
                    source.Contains("clr-namespace:PCL.Application", StringComparison.Ordinal) ||
                    source.Contains("clr-namespace:PCL.Desktop", StringComparison.Ordinal) ||
                    source.Contains("clr-namespace:PCL.Plugin", StringComparison.Ordinal))
                    throw new InvalidOperationException($"AXAML contains code-behind or a private host namespace: {packagePath}");
                AddFile(files, packagePath, sourcePath);
            }
        }
    }

    private static void AddFile(IDictionary<string, byte[]> files, string packagePath, string sourcePath)
    {
        string normalized = NormalizePath(packagePath);
        if (files.ContainsKey(normalized)) throw new InvalidOperationException($"Duplicate package path: {normalized}");
        files[normalized] = File.ReadAllBytes(Path.GetFullPath(sourcePath));
    }

    private static void Validate(IReadOnlyDictionary<string, byte[]> files)
    {
        HashSet<string> paths = new(StringComparer.OrdinalIgnoreCase);
        foreach (string path in files.Keys)
        {
            if (!paths.Add(path)) throw new InvalidOperationException($"Package paths differ only by case: {path}");
            if (ForbiddenAssemblies.Contains(Path.GetFileName(path)))
                throw new InvalidOperationException($"Shared or privileged assembly must not be packaged: {path}");
        }
    }

    private static string NormalizePath(string path)
    {
        string value = path.Replace('\\', '/');
        if (string.IsNullOrWhiteSpace(value) || value.StartsWith('/') || value.Contains(':') ||
            value.Split('/').Any(static part => part.Length == 0 || part is "." or ".."))
            throw new InvalidOperationException($"Unsafe package path: {path}");
        return value;
    }

    private static string RequiredString(JsonElement element, string name) =>
        element.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
            ? value.GetString()! : throw new InvalidOperationException($"Manifest property '{name}' is required.");

    private static string Hash(ReadOnlySpan<byte> value) => Convert.ToHexString(SHA256.HashData(value)).ToLowerInvariant();

    private static string ComputePayloadRoot(IReadOnlyList<FileRecord> records)
    {
        List<byte[]> level = new(records.Count);
        foreach (FileRecord record in records)
        {
            byte[] path = Encoding.UTF8.GetBytes(record.Path);
            byte[] digest = Convert.FromHexString(record.Sha256);
            byte[] leaf = new byte[1 + path.Length + 1 + sizeof(long) + digest.Length];
            leaf[0] = 0;
            path.CopyTo(leaf, 1);
            int offset = 1 + path.Length;
            leaf[offset++] = 0;
            BinaryPrimitives.WriteInt64BigEndian(leaf.AsSpan(offset, sizeof(long)), record.Size);
            digest.CopyTo(leaf, offset + sizeof(long));
            level.Add(SHA256.HashData(leaf));
        }
        while (level.Count > 1)
        {
            List<byte[]> next = new((level.Count + 1) / 2);
            for (int index = 0; index < level.Count; index += 2)
            {
                byte[] left = level[index];
                byte[] right = index + 1 < level.Count ? level[index + 1] : left;
                byte[] node = new byte[1 + left.Length + right.Length];
                node[0] = 1;
                left.CopyTo(node, 1);
                right.CopyTo(node, 1 + left.Length);
                next.Add(SHA256.HashData(node));
            }
            level = next;
        }
        return Convert.ToHexString(level[0]).ToLowerInvariant();
    }

    private static string GetDefaultDevelopmentGpgHome()
    {
        string basePath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(basePath))
            basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
        return Path.Combine(basePath, "PCL-N", "plugin-sdk", "development-gpg");
    }
    private sealed record FileRecord(string Path, long Size, string Sha256);

    private sealed class Utf8PathComparer : IComparer<string>
    {
        public static Utf8PathComparer Instance { get; } = new();
        public int Compare(string? x, string? y) => x is null ? y is null ? 0 : -1 : y is null ? 1 :
            Encoding.UTF8.GetBytes(x).AsSpan().SequenceCompareTo(Encoding.UTF8.GetBytes(y));
    }
}
