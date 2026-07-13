using System.Diagnostics;

internal static class GpgSigner
{
    public static async Task SignAsync(
        IDictionary<string, byte[]> files,
        byte[] signedObject,
        string fingerprint,
        string gpgPath)
    {
        string directory = Path.Combine(Path.GetTempPath(), "pcl-n-pnp-sign-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            string source = Path.Combine(directory, "pnp.signed.json");
            string signature = Path.Combine(directory, fingerprint + ".asc");
            string key = Path.Combine(directory, fingerprint + ".key.asc");
            await File.WriteAllBytesAsync(source, signedObject).ConfigureAwait(false);
            await RunAsync(gpgPath, ["--batch", "--yes", "--armor", "--detach-sign", "--local-user", fingerprint, "--output", signature, source]).ConfigureAwait(false);
            await RunAsync(gpgPath, ["--batch", "--yes", "--armor", "--export", "--output", key, fingerprint]).ConfigureAwait(false);
            files[$"META-INF/signatures/{fingerprint}.asc"] = await File.ReadAllBytesAsync(signature).ConfigureAwait(false);
            files[$"META-INF/keys/{fingerprint}.asc"] = await File.ReadAllBytesAsync(key).ConfigureAwait(false);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static async Task RunAsync(string fileName, IReadOnlyList<string> arguments)
    {
        ProcessStartInfo startInfo = new(fileName)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        foreach (string argument in arguments) startInfo.ArgumentList.Add(argument);
        using Process process = Process.Start(startInfo) ?? throw new InvalidOperationException($"Unable to start {fileName}.");
        string error = await process.StandardError.ReadToEndAsync().ConfigureAwait(false);
        await process.WaitForExitAsync().ConfigureAwait(false);
        if (process.ExitCode != 0)
            throw new InvalidOperationException($"{fileName} failed with exit code {process.ExitCode}: {error.Trim()}");
    }
}
