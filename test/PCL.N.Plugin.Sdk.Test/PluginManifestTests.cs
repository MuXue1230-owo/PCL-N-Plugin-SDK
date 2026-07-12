using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin.Sdk;

namespace PCL.N.Plugin.Sdk.Test;

[TestClass]
public sealed class PluginManifestTests
{
    [TestMethod]
    public void ValidManifest_ParsesAndValidates()
    {
        PluginManifestValidationResult result = PluginManifestValidator.ParseAndValidate(Encoding.UTF8.GetBytes(ValidManifest));

        Assert.IsTrue(result.IsValid, string.Join(Environment.NewLine, result.Issues));
        Assert.AreEqual("dev.muxue.example", result.Manifest!.Id);
    }

    [TestMethod]
    public void InvalidManifest_ReportsIdentityPathAndSignatureProblems()
    {
        string invalid = ValidManifest
            .Replace("dev.muxue.example", "pcl.invalid", StringComparison.Ordinal)
            .Replace("lib/net10.0/Example.Plugin.dll", "../Example.Plugin.dll", StringComparison.Ordinal)
            .Replace(new string('A', 40), "1234", StringComparison.Ordinal);

        PluginManifestValidationResult result = PluginManifestValidator.ParseAndValidate(Encoding.UTF8.GetBytes(invalid));

        Assert.IsFalse(result.IsValid);
        CollectionAssert.IsSubsetOf(
            new[] { "PNPMAN005", "PNPMAN010", "PNPMAN017", "PNPMAN019" },
            result.Issues.Select(issue => issue.Code).ToArray());
    }

    [TestMethod]
    public void PluginVersion_FollowsSemVerPreReleaseOrdering()
    {
        Assert.IsTrue(PluginVersion.Parse("1.0.0-alpha.1") < PluginVersion.Parse("1.0.0-beta"));
        Assert.IsTrue(PluginVersion.Parse("1.0.0-rc.1") < PluginVersion.Parse("1.0.0"));
        Assert.AreEqual(PluginVersion.Parse("1.2.3"), PluginVersion.Parse("1.2.3"));
        Assert.IsTrue(PluginVersion.Parse("1.0.0-99999999999999999999") > PluginVersion.Parse("1.0.0-2"));
        Assert.IsFalse(PluginVersion.TryParse("1.0", out _));
        Assert.IsFalse(PluginVersion.TryParse("1.0.0-01", out _));
    }

    [TestMethod]
    public void PluginVersionRange_EvaluatesConjunctions()
    {
        PluginVersionRange range = PluginVersionRange.Parse(">=2.0.0 <3.0.0");

        Assert.IsTrue(range.Contains(PluginVersion.Parse("2.5.1")));
        Assert.IsFalse(range.Contains(PluginVersion.Parse("3.0.0")));
    }

    private const string ValidManifest = """
        {
          "formatVersion": 1,
          "manifestVersion": 1,
          "id": "dev.muxue.example",
          "name": "Example Plugin",
          "version": "1.2.0-beta.1",
          "channel": "beta",
          "publisher": { "id": "github:MuXue1230-owo", "namespace": "dev.muxue" },
          "license": "Apache-2.0",
          "entryPoint": {
            "assembly": "lib/net10.0/Example.Plugin.dll",
            "type": "Example.Plugin.PluginEntry"
          },
          "api": { "minimum": "1.0", "maximumExclusive": "2.0" },
          "host": { "minimumVersion": "1.0.0", "maximumVersionExclusive": null },
          "platforms": {
            "operatingSystems": ["windows", "linux", "macos"],
            "architectures": ["x64", "arm64"]
          },
          "dependencies": [
            { "id": "dev.example.library", "version": ">=2.0.0 <3.0.0", "kind": "required" }
          ],
          "permissions": [{ "id": "network", "reason": "Fetch remote data." }],
          "data": { "schemaVersion": 4, "minimumReadableSchema": 3 },
          "signing": { "fingerprint": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" }
        }
        """;
}
