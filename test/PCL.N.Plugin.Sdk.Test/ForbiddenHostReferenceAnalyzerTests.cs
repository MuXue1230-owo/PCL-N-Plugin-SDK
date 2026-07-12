using System.Collections.Immutable;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.N.Plugin.Analyzers;

namespace PCL.N.Plugin.Sdk.Test;

[TestClass]
public sealed class ForbiddenHostReferenceAnalyzerTests
{
    [TestMethod]
    public async Task Reports_PNPSDK001_WhenReferencingApplication()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            source: "class C { }",
            referencedAssemblyName: "PCL.Application");

        Assert.IsTrue(diagnostics.Any(d => d.Id == ForbiddenHostReferenceAnalyzer.ApplicationRuleId));
    }

    [TestMethod]
    public async Task Reports_PNPSDK002_WhenReferencingDesktop()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            source: "class C { }",
            referencedAssemblyName: "PCL.Desktop");

        Assert.IsTrue(diagnostics.Any(d => d.Id == ForbiddenHostReferenceAnalyzer.DesktopRuleId));
    }

    [TestMethod]
    public async Task Reports_PNPSDK003_WhenReferencingPlugin()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            source: "class C { }",
            referencedAssemblyName: "PCL.Plugin");

        Assert.IsTrue(diagnostics.Any(d => d.Id == ForbiddenHostReferenceAnalyzer.PluginRuleId));
    }

    [TestMethod]
    public async Task AllowsAbstractionsOnly()
    {
        ImmutableArray<Diagnostic> diagnostics = await AnalyzeAsync(
            source: "class C { }",
            referencedAssemblyName: "PCL.N.Plugin.Abstractions");

        Assert.AreEqual(0, diagnostics.Length);
    }

    private static async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(string source, string referencedAssemblyName)
    {
        SyntaxTree tree = CSharpSyntaxTree.ParseText(source);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "AnalyzerVictim",
            [tree],
            [
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                // Empty netmodule-style reference identity via CompilationReference is awkward;
                // use a minimal emit of a dummy assembly with the forbidden name.
                CreateNamedReference(referencedAssemblyName)
            ],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        ForbiddenHostReferenceAnalyzer analyzer = new();
        CompilationWithAnalyzers withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        return await withAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
    }

    private static MetadataReference CreateNamedReference(string assemblyName)
    {
        CSharpCompilation empty = CSharpCompilation.Create(
            assemblyName,
            [CSharpSyntaxTree.ParseText("namespace Dummy { public class Marker { } }")],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)],
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream stream = new();
        Microsoft.CodeAnalysis.Emit.EmitResult emit = empty.Emit(stream);
        Assert.IsTrue(emit.Success, string.Join("; ", emit.Diagnostics.Select(d => d.ToString())));
        stream.Position = 0;
        return MetadataReference.CreateFromStream(stream);
    }
}
