using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin;
using PCL.N.Plugin.Analyzers;

namespace PCL.N.Plugin.Sdk.Test;

[TestClass]
public sealed class PluginEntryTypeAnalyzerTests
{
    [TestMethod]
    public async Task PNPSDK004_ReportsInvalidEntryShape()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            """
            using PCL.N.Plugin;
            internal abstract class BadEntry : IPclNPlugin
            {
                public System.Threading.Tasks.ValueTask InitializeAsync(IPluginContext context, System.Threading.CancellationToken cancellationToken)
                    => default;
                public System.Threading.Tasks.ValueTask ShutdownAsync(System.Threading.CancellationToken cancellationToken)
                    => default;
            }
            """);

        Assert.IsTrue(diagnostics.Any(d => d.Id == PluginEntryTypeAnalyzer.RuleId));
    }

    [TestMethod]
    public async Task PNPSDK004_AllowsValidPublicEntry()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            """
            using PCL.N.Plugin;
            public sealed class GoodEntry : IPclNPlugin
            {
                public System.Threading.Tasks.ValueTask InitializeAsync(IPluginContext context, System.Threading.CancellationToken cancellationToken)
                    => default;
                public System.Threading.Tasks.ValueTask ShutdownAsync(System.Threading.CancellationToken cancellationToken)
                    => default;
            }
            """);

        Assert.IsFalse(diagnostics.Any(d => d.Id == PluginEntryTypeAnalyzer.RuleId));
    }

    [TestMethod]
    public async Task PNPSDK010_ReportsTaskRun()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            """
            using System.Threading.Tasks;
            public sealed class C
            {
                public void M() => Task.Run(() => { });
            }
            """);

        Assert.IsTrue(diagnostics.Any(d => d.Id == ForbiddenBackgroundWorkAnalyzer.RuleId));
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        string runtime = typeof(object).Assembly.Location;
        string privateCore = Path.Combine(Path.GetDirectoryName(runtime)!, "System.Runtime.dll");
        string collections = Path.Combine(Path.GetDirectoryName(runtime)!, "System.Collections.dll");
        string threading = Path.Combine(Path.GetDirectoryName(runtime)!, "System.Threading.dll");
        string tasks = typeof(Task).Assembly.Location;
        string abstractions = typeof(IPclNPlugin).Assembly.Location;

        List<MetadataReference> references =
        [
            MetadataReference.CreateFromFile(runtime),
            MetadataReference.CreateFromFile(privateCore),
            MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
            MetadataReference.CreateFromFile(tasks),
            MetadataReference.CreateFromFile(abstractions),
            MetadataReference.CreateFromFile(Assembly.Load("System.Threading.Tasks").Location)
        ];
        if (File.Exists(collections))
            references.Add(MetadataReference.CreateFromFile(collections));
        if (File.Exists(threading))
            references.Add(MetadataReference.CreateFromFile(threading));

        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerVictim",
            new[] { tree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ImmutableArray<DiagnosticAnalyzer> analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
            new PluginEntryTypeAnalyzer(),
            new ForbiddenBackgroundWorkAnalyzer());
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(analyzers);
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }
}
