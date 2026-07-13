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
        Assert.AreEqual("pcl.page.launch", result.Manifest.Ui!.Targets.Single().Target);
        Assert.AreEqual("primary-actions.after", result.Manifest.Ui.Targets.Single().Operations.Single().Slot);
        Assert.AreEqual("ui/LaunchPanel.axaml", result.Manifest.Ui.Targets.Single().Operations.Single().Axaml);
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
    public void PluginApiVersion_ParsesAndRejectsInvalidValues()
    {
        Assert.AreEqual(new PluginApiVersion(1, 2), PluginApiVersion.Parse("1.2"));
        Assert.IsFalse(PluginApiVersion.TryParse("1", out _));
        Assert.IsFalse(PluginApiVersion.TryParse("-1.0", out _));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => new PluginApiVersion(-1, 0));
    }

    [TestMethod]
    public void InvalidUiManifest_ReportsMissingInjectSlot()
    {
        string invalid = ValidManifest.Replace(
            "\"slot\": \"primary-actions.after\",",
            "\"slot\": null,",
            StringComparison.Ordinal);

        PluginManifestValidationResult result = PluginManifestValidator.ParseAndValidate(Encoding.UTF8.GetBytes(invalid));

        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "PNPMAN038"));
    }

    [TestMethod]
    public void InvalidAxamlPath_IsRejected()
    {
        string invalid = ValidManifest.Replace("ui/LaunchPanel.axaml", "../LaunchPanel.axaml", StringComparison.Ordinal);
        PluginManifestValidationResult result = PluginManifestValidator.ParseAndValidate(Encoding.UTF8.GetBytes(invalid));
        Assert.IsTrue(result.Issues.Any(issue => issue.Code == "PNPMAN040"));
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
          "ui": {
            "api": { "minimum": "1.0", "maximumExclusive": "2.0" },
            "avalonia": { "minimum": "12.0", "maximumExclusive": "13.0" },
            "requiresRestart": false,
            "targets": [{
              "target": "pcl.page.launch",
              "surface": ">=3.0.0 <4.0.0",
              "access": ["observe", "inject"],
              "operations": [{
                "id": "launch-panel",
                "kind": "inject",
                "slot": "primary-actions.after",
                "axaml": "ui/LaunchPanel.axaml",
                "command": "example.hello.say-hello",
                "priority": 220,
                "required": true,
                "fallback": "disable-feature"
              }]
            }]
          },
          "data": { "schemaVersion": 4, "minimumReadableSchema": 3 },
          "signing": { "fingerprint": "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" }
        }
        """;
}
