using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PCL.N.Plugin.Analyzers;

/// <summary>
/// PNPSDK001–003: third-party plugins must not reference privileged host assemblies
/// (design §3 / §7). Package-time packaging bans are enforced separately by PCL.Plugin.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbiddenHostReferenceAnalyzer : DiagnosticAnalyzer
{
    public const string ApplicationRuleId = "PNPSDK001";
    public const string DesktopRuleId = "PNPSDK002";
    public const string PluginRuleId = "PNPSDK003";

    private static readonly DiagnosticDescriptor ApplicationRule = CreateRule(
        ApplicationRuleId,
        "Do not reference PCL.Application",
        "Third-party plugins must not reference assembly '{0}'. Use PCL.N.Plugin.Abstractions stable services instead.");

    private static readonly DiagnosticDescriptor DesktopRule = CreateRule(
        DesktopRuleId,
        "Do not reference PCL.Desktop",
        "Third-party plugins must not reference assembly '{0}'. UI extension goes through declared plugin surfaces.");

    private static readonly DiagnosticDescriptor PluginRule = CreateRule(
        PluginRuleId,
        "Do not reference PCL.Plugin",
        "Third-party plugins must not reference assembly '{0}'. PCL.Plugin is the privileged host runtime.");

    private static readonly ImmutableDictionary<string, DiagnosticDescriptor> RulesByAssembly =
        ImmutableDictionary.CreateRange(StringComparer.OrdinalIgnoreCase, new[]
        {
            new KeyValuePair<string, DiagnosticDescriptor>("PCL.Application", ApplicationRule),
            new KeyValuePair<string, DiagnosticDescriptor>("PCL.Desktop", DesktopRule),
            new KeyValuePair<string, DiagnosticDescriptor>("PCL.Plugin", PluginRule)
        });

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(ApplicationRule, DesktopRule, PluginRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext context)
    {
        foreach (AssemblyIdentity identity in context.Compilation.ReferencedAssemblyNames)
        {
            if (!RulesByAssembly.TryGetValue(identity.Name, out DiagnosticDescriptor? rule))
                continue;

            context.ReportDiagnostic(Diagnostic.Create(rule, Location.None, identity.Name));
        }
    }

    private static DiagnosticDescriptor CreateRule(string id, string title, string message) =>
        new(
            id,
            title,
            message,
            category: "PCL.N.Plugin.Security",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description:
            "Privileged host assemblies are only for the PCL N launcher and PCL.Plugin. " +
            "Third-party plugins consume the public ABI in PCL.N.Plugin.Abstractions.");
}
