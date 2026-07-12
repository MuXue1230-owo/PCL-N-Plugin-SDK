using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PCL.N.Plugin.Analyzers;

/// <summary>
/// PNPSDK006: forbid using / qualifying privileged host namespaces in plugin source
/// (complements assembly-level PNPSDK001–003).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbiddenNamespaceAnalyzer : DiagnosticAnalyzer
{
    public const string RuleId = "PNPSDK006";

    private static readonly string[] ForbiddenPrefixes =
    [
        "PCL.Application",
        "PCL.Desktop",
        "PCL.Plugin"
    ];

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        "Do not use privileged host namespaces",
        "Namespace '{0}' is not part of the public plugin ABI; use PCL.N.Plugin contracts instead",
        category: "PCL.N.Plugin.Security",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Third-party plugins must not consume launcher-internal namespaces.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeUsing, SyntaxKind.UsingDirective);
        context.RegisterSyntaxNodeAction(AnalyzeQualifiedName, SyntaxKind.QualifiedName);
    }

    private static void AnalyzeUsing(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not UsingDirectiveSyntax usingDirective || usingDirective.Name is null)
            return;

        string name = usingDirective.Name.ToString();
        if (IsForbidden(name))
            context.ReportDiagnostic(Diagnostic.Create(Rule, usingDirective.GetLocation(), name));
    }

    private static void AnalyzeQualifiedName(SyntaxNodeAnalysisContext context)
    {
        if (context.Node is not QualifiedNameSyntax qualified)
            return;

        // Only report top-level type references that start with a forbidden root.
        if (qualified.Parent is QualifiedNameSyntax)
            return;

        string name = qualified.ToString();
        if (IsForbidden(name))
            context.ReportDiagnostic(Diagnostic.Create(Rule, qualified.GetLocation(), name));
    }

    private static bool IsForbidden(string name)
    {
        foreach (string prefix in ForbiddenPrefixes)
        {
            if (name.Equals(prefix, StringComparison.Ordinal) ||
                name.StartsWith(prefix + ".", StringComparison.Ordinal))
            {
                // Allow documenting the public plugin abstractions under PCL.N.Plugin.
                if (name.StartsWith("PCL.Plugin", StringComparison.Ordinal) &&
                    !name.Equals("PCL.Plugin", StringComparison.Ordinal) &&
                    !name.StartsWith("PCL.Plugin.", StringComparison.Ordinal))
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }
}
