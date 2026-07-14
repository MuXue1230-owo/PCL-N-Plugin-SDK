using System.Diagnostics;
using System.Globalization;

internal static class GpgSigner
{
    public static async Task SignAsync(
        IDictionary<string, byte[]> files,
        byte[] signedObject,
        string fingerprint,
        string gpgPath,
        string? gpgHome = null)
    {
        string directory = Path.Combine(Path.GetTempPath(), "pcl-n-pnp-sign-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string source = Path.Combine(directory, "pnp.signed.json");
            string signature = Path.Combine(directory, fingerprint + ".asc");
            string key = Path.Combine(directory, fingerprint + ".key.asc");
            await File.WriteAllBytesAsync(source, signedObject).ConfigureAwait(false);
            // OpenPGP embeds its creation time in the signature. Freeze it immediately after the newest
            // secret-key packet became valid so identical inputs and the same key produce identical packages.
            long signatureEpoch = await GetStableSignatureEpochAsync(gpgPath, gpgHome, fingerprint).ConfigureAwait(false);
            await RunAsync(gpgPath, WithHome(gpgHome,
            [
                "--batch", "--yes", "--faked-system-time", signatureEpoch.ToString(CultureInfo.InvariantCulture) + "!",
                "--armor", "--detach-sign", "--local-user", fingerprint, "--output", signature, source
            ])).ConfigureAwait(false);
            await RunAsync(gpgPath, WithHome(gpgHome, ["--batch", "--yes", "--armor", "--export", "--output", key, fingerprint])).ConfigureAwait(false);
            files[$"META-INF/signatures/{fingerprint}.asc"] = await File.ReadAllBytesAsync(signature).ConfigureAwait(false);
            files[$"META-INF/keys/{fingerprint}.asc"] = await File.ReadAllBytesAsync(key).ConfigureAwait(false);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    public static async Task<string> GetOrCreateDevelopmentFingerprintAsync(string gpgPath, string gpgHome)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gpgPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(gpgHome);
        Directory.CreateDirectory(gpgHome);
        string? fingerprint = await FindFingerprintAsync(gpgPath, gpgHome).ConfigureAwait(false);
        if (fingerprint is not null)
            return fingerprint;

        await RunAsync(gpgPath, WithHome(gpgHome,
        [
            "--batch", "--yes", "--pinentry-mode", "loopback", "--passphrase", string.Empty,
            "--quick-generate-key", "PCL N Local Development <pcln-development@localhost>",
            "future-default", "default", "never"
        ])).ConfigureAwait(false);
        return await FindFingerprintAsync(gpgPath, gpgHome).ConfigureAwait(false)
            ?? throw new InvalidOperationException("gpg generated a development key but did not report its fingerprint.");
    }

    private static async Task<string?> FindFingerprintAsync(string gpgPath, string gpgHome)
    {
        string output = await RunCaptureAsync(
            gpgPath,
            WithHome(gpgHome, ["--batch", "--with-colons", "--fingerprint", "--list-secret-keys"]),
            allowFailure: true).ConfigureAwait(false);
        foreach (string line in output.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] fields = line.Split(':');
            if (fields.Length > 9 && fields[0] == "fpr" && fields[9].Length >= 40 && fields[9].All(Uri.IsHexDigit))
                return fields[9].ToUpperInvariant();
        }
        return null;
    }

    private static async Task<long> GetStableSignatureEpochAsync(string gpgPath, string? gpgHome, string fingerprint)
    {
        string output = await RunCaptureAsync(
            gpgPath,
            WithHome(gpgHome, ["--batch", "--with-colons", "--list-secret-keys", fingerprint]),
            allowFailure: false).ConfigureAwait(false);
        return ReadLatestSecretKeyCreationEpoch(output) + 1;
    }

    internal static long ReadLatestSecretKeyCreationEpoch(string colonListing)
    {
        long latest = 0;
        foreach (string line in colonListing.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string[] fields = line.Split(':');
            if (fields.Length > 5 && fields[0] is "sec" or "ssb" &&
                long.TryParse(fields[5], NumberStyles.None, CultureInfo.InvariantCulture, out long created))
            {
                latest = Math.Max(latest, created);
            }
        }
        return latest > 0
            ? latest
            : throw new InvalidOperationException("gpg did not report a secret key creation time for deterministic signing.");
    }

    private static IReadOnlyList<string> WithHome(string? gpgHome, IReadOnlyList<string> arguments)
    {
        if (string.IsNullOrWhiteSpace(gpgHome))
            return arguments;
        List<string> result = ["--homedir", gpgHome];
        result.AddRange(arguments);
        return result;
    }

    private static async Task RunAsync(string fileName, IReadOnlyList<string> arguments)
    {
        _ = await RunCaptureAsync(fileName, arguments, allowFailure: false).ConfigureAwait(false);
    }

    private static async Task<string> RunCaptureAsync(
        string fileName,
        IReadOnlyList<string> arguments,
        bool allowFailure)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        bool useMsysPaths = OperatingSystem.IsWindows() && UsesMsysPathSyntax(fileName);
        foreach (string argument in arguments)
            startInfo.ArgumentList.Add(NormalizeArgument(argument, OperatingSystem.IsWindows(), useMsysPaths));
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Unable to start {fileName}.");
        string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        string output = await process.StandardOutput.ReadToEndAsync().ConfigureAwait(false);
        if (process.ExitCode != 0 && !allowFailure)
            throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}: {error.Trim()}");
        return output;
    }

    internal static string NormalizeArgument(string argument, bool isWindows, bool useMsysPaths = false)
    {
        if (!isWindows)
            return argument;
        string normalized = argument.Replace('\\', '/');
        if (useMsysPaths && normalized.Length >= 3 &&
            char.IsAsciiLetter(normalized[0]) && normalized[1] == ':' && normalized[2] == '/')
        {
            return $"/{char.ToLowerInvariant(normalized[0])}{normalized[2..]}";
        }
        return normalized;
    }

    private static bool UsesMsysPathSyntax(string fileName)
    {
        string? executable = ResolveExecutable(fileName);
        if (executable is null)
            return false;
        string normalized = executable.Replace('/', '\\');
        return normalized.Contains("\\Git\\usr\\bin\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\msys64\\", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("\\mingw64\\", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ResolveExecutable(string fileName)
    {
        if (Path.IsPathRooted(fileName) || fileName.Contains(Path.DirectorySeparatorChar) ||
            fileName.Contains(Path.AltDirectorySeparatorChar))
        {
            return File.Exists(fileName) ? Path.GetFullPath(fileName) : null;
        }

        foreach (string directory in (Environment.GetEnvironmentVariable("PATH") ?? string.Empty)
                     .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            string candidate = Path.Combine(directory.Trim('"'), fileName);
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
            if (!fileName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) && File.Exists(candidate + ".exe"))
                return Path.GetFullPath(candidate + ".exe");
        }
        return null;
    }
}
