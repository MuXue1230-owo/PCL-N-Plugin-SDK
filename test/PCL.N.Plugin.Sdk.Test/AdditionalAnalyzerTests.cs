using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin;
using PCL.N.Plugin.Analyzers;
using PCL.N.Plugin.Sdk;

namespace PCL.N.Plugin.Sdk.Test;

[TestClass]
public sealed class AdditionalAnalyzerTests
{
    [TestMethod]
    public async Task PNPSDK006_ReportsForbiddenUsing()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            "using PCL.Application.Hosting;\nclass C {}",
            new ForbiddenNamespaceAnalyzer());

        Assert.IsTrue(diagnostics.Any(d => d.Id == ForbiddenNamespaceAnalyzer.RuleId));
    }

    [TestMethod]
    public async Task PNPSDK005_ReportsDiscardedRegistration()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            """
            using PCL.N.Plugin;
            using System.Threading.Tasks;
            class C {
              void M(IPluginCommandService commands) {
                commands.Register(new PluginCommandDescriptor("id", "t", _ => Task.CompletedTask));
              }
            }
            """,
            new LifetimeTrackAnalyzer());

        Assert.IsTrue(diagnostics.Any(d => d.Id == LifetimeTrackAnalyzer.RuleId));
    }

    [TestMethod]
    public void PNPSDK008_ManifestMissingApiRange()
    {
        // Empty api object fields via incomplete JSON is hard; validate null-like empty strings.
        PluginManifest manifest = MinimalManifest() with
        {
            Api = new PluginApiRangeManifest("", "")
        };
        PluginManifestValidationResult result = PluginManifestValidator.Validate(manifest);
        Assert.IsTrue(result.Issues.Any(i => i.Code == "PNPSDK008"));
    }

    private static PluginManifest MinimalManifest() =>
        new()
        {
            FormatVersion = 1,
            ManifestVersion = 1,
            Id = "dev.example.plugin",
            Name = "Example",
            Version = "1.0.0",
            Publisher = new PluginPublisherManifest("example", "dev.example"),
            License = "Apache-2.0",
            EntryPoint = new PluginEntryPointManifest("lib/net10.0/Example.dll", "Example.Entry"),
            Api = new PluginApiRangeManifest("0.1", "1.0"),
            Host = new PluginHostRangeManifest("1.0.0"),
            Signing = new PluginSigningManifest("AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA")
        };

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, params DiagnosticAnalyzer[] analyzers)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        string runtime = typeof(object).Assembly.Location;
        string privateCore = Path.Combine(Path.GetDirectoryName(runtime)!, "System.Runtime.dll");
        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(runtime),
            MetadataReference.CreateFromFile(privateCore),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Task).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(IPclNPlugin).Assembly.Location),
            MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Tasks").Location)
        ];

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerVictim",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create(analyzers));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }
}
