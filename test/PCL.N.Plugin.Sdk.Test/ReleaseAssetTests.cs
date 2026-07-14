using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin.Analyzers;

namespace PCL.N.Plugin.Sdk.Test;

[TestClass]
public sealed class ReleaseAssetTests
{
    [TestMethod]
    public void Wiki_InternalLinksResolveAndPagesAreVersioned()
    {
        string root = FindRepositoryRoot();
        string wiki = Path.Combine(root, "wiki");
        HashSet<string> pages = Directory.EnumerateFiles(wiki, "*.md")
            .Select(static file => Path.GetFileNameWithoutExtension(file)!)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        List<string> errors = [];
        foreach (string file in Directory.EnumerateFiles(wiki, "*.md"))
        {
            string text = File.ReadAllText(file);
            if (!text.Contains("0.1.0-alpha.4", StringComparison.Ordinal))
                errors.Add(Path.GetFileName(file) + ": missing SDK version");
            foreach (Match link in Regex.Matches(text, "\\[\\[([^]#|]+)"))
            {
                if (!pages.Contains(link.Groups[1].Value))
                    errors.Add(Path.GetFileName(file) + ": missing link " + link.Groups[1].Value);
            }
        }
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors));
    }

    [TestMethod]
    public void Packages_UseExpectedNuGetIds()
    {
        string root = FindRepositoryRoot();
        Dictionary<string, string> packages = new(StringComparer.Ordinal)
        {
            ["PCL.N.Plugin.Abstractions"] = "PCLN.Plugin.Abstractions",
            ["PCL.N.Plugin.Analyzers"] = "PCLN.Plugin.Analyzers",
            ["PCL.N.Plugin.Sdk"] = "PCLN.Plugin.Sdk",
            ["PCL.N.Plugin.Sdk.Build"] = "PCLN.Plugin.Sdk.Build",
            ["PCL.N.Plugin.Testing"] = "PCLN.Plugin.Testing"
        };

        foreach ((string project, string packageId) in packages)
        {
            string path = Path.Combine(root, "src", project, project + ".csproj");
            string text = File.ReadAllText(path);
            StringAssert.Contains(text, $"<PackageId>{packageId}</PackageId>");
        }
    }

    [TestMethod]
    public void ReleaseWorkflow_UsesNuGetTrustedPublishing()
    {
        string root = FindRepositoryRoot();
        string workflow = File.ReadAllText(Path.Combine(root, ".github", "workflows", "release.yml"));

        StringAssert.Contains(workflow, "id-token: write");
        StringAssert.Contains(workflow, "uses: NuGet/login@v1");
        StringAssert.Contains(workflow, "user: ${{ vars.NUGET_USER }}");
        StringAssert.Contains(workflow, "steps.nuget-login.outputs.NUGET_API_KEY");
        Assert.IsFalse(workflow.Contains("secrets.NUGET_API_KEY", StringComparison.Ordinal));
    }

    [TestMethod]
    public void Wiki_ListsEveryAnalyzerDiagnostic()
    {
        string root = FindRepositoryRoot();
        string text = File.ReadAllText(Path.Combine(root, "wiki", "Analyzer-Reference.md"));
        string[] ids =
        [
            ForbiddenHostReferenceAnalyzer.ApplicationRuleId,
            ForbiddenHostReferenceAnalyzer.DesktopRuleId,
            ForbiddenHostReferenceAnalyzer.PluginRuleId,
            PluginEntryTypeAnalyzer.RuleId,
            LifetimeTrackAnalyzer.RuleId,
            ForbiddenNamespaceAnalyzer.RuleId,
            PluginManifestAdditionalFileAnalyzer.ManifestRuleId,
            PluginManifestAdditionalFileAnalyzer.ApiRuleId,
            PluginManifestAdditionalFileAnalyzer.UiRuleId,
            ForbiddenBackgroundWorkAnalyzer.RuleId
        ];
        foreach (string id in ids) StringAssert.Contains(text, id);
    }

    [TestMethod]
    public void BuildPackage_ContainsExpectedPackAssets()
    {
        string root = FindRepositoryRoot();
        string project = Path.Combine(root, "src", "PCL.N.Plugin.Sdk.Build");
        foreach (string relative in new[]
        {
            "build/PCLN.Plugin.Sdk.Build.props",
            "build/PCLN.Plugin.Sdk.Build.targets",
            "buildTransitive/PCLN.Plugin.Sdk.Build.props",
            "buildTransitive/PCLN.Plugin.Sdk.Build.targets"
        })
            Assert.IsTrue(File.Exists(Path.Combine(project, relative)), relative);
    }

    [TestMethod]
    public void ExampleAxaml_IsDeclarativeAndManifestDeclared()
    {
        string root = FindRepositoryRoot();
        string axaml = File.ReadAllText(Path.Combine(root, "examples", "HelloPlugin", "ui", "HelloPanel.axaml"));
        string manifest = File.ReadAllText(Path.Combine(root, "examples", "HelloPlugin", "plugin.json"));
        Assert.IsFalse(axaml.Contains("x:Class=", StringComparison.Ordinal));
        Assert.IsFalse(axaml.Contains("clr-namespace:PCL.", StringComparison.Ordinal));
        StringAssert.Contains(manifest, "ui/HelloPanel.axaml");
    }

    private static string FindRepositoryRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "PCL-N-Plugin-SDK.slnx")))
                return directory.FullName;
            directory = directory.Parent;
        }
        throw new DirectoryNotFoundException("SDK repository root was not found.");
    }
}
