internal sealed record PackOptions(
    string ManifestPath,
    string AssemblyPath,
    string? DepsPath,
    string OutputPath,
    string ContentRoot,
    IReadOnlyList<string> ContentFiles,
    IReadOnlyList<string> Dependencies,
    IReadOnlyList<string> NativeFiles,
    bool Sign,
    string GpgPath,
    string? DevelopmentGpgHome)
{
    public static PackOptions Parse(string[] args)
    {
        Dictionary<string, List<string>> values = new(StringComparer.OrdinalIgnoreCase);
        for (int index = 0; index < args.Length; index++)
        {
            string key = args[index];
            if (!key.StartsWith("--", StringComparison.Ordinal) || index + 1 >= args.Length)
                throw new ArgumentException($"Invalid argument: {key}");
            if (!values.TryGetValue(key, out List<string>? list)) values[key] = list = [];
            list.Add(args[++index]);
        }

        string Required(string name) => values.TryGetValue(name, out List<string>? list) && list.Count > 0
            ? Path.GetFullPath(list[^1]) : throw new ArgumentException($"Missing required option {name}.");
        string? Optional(string name) => values.TryGetValue(name, out List<string>? list) && list.Count > 0
            ? Path.GetFullPath(list[^1]) : null;

        string manifest = Required("--manifest");
        string root = Optional("--content-root") ?? Path.GetDirectoryName(manifest)!;
        IReadOnlyList<string> content = values.TryGetValue("--content", out List<string>? files) ? files : [];
        IReadOnlyList<string> dependencies = values.TryGetValue("--dependency", out List<string>? dependencyFiles) ? dependencyFiles : [];
        IReadOnlyList<string> nativeFiles = values.TryGetValue("--native", out List<string>? nativeValues) ? nativeValues : [];
        bool sign = values.TryGetValue("--sign", out List<string>? signValues) &&
                    bool.TryParse(signValues[^1], out bool enabled) && enabled;
        string gpg = values.TryGetValue("--gpg", out List<string>? gpgValues) ? gpgValues[^1] : "gpg";
        string? developmentGpgHome = Optional("--development-gpg-home");
        return new PackOptions(manifest, Required("--assembly"), Optional("--deps"), Required("--output"), root, content, dependencies, nativeFiles, sign, gpg, developmentGpgHome);
    }
}
