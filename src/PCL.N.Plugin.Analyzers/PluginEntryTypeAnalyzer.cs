using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PCL.N.Plugin.Analyzers;

/// <summary>
/// PNPSDK004: types that implement <c>IPclNPlugin</c> must be public, non-abstract,
/// non-generic, and expose a public parameterless constructor (design §7.6).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PluginEntryTypeAnalyzer : DiagnosticAnalyzer
{
    public const string RuleId = "PNPSDK004";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        "IPclNPlugin entry type shape is invalid",
        "Type '{0}' implements IPclNPlugin but is not a public non-abstract non-generic type with a public parameterless constructor",
        category: "PCL.N.Plugin.Lifecycle",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Each .pnp package may expose one public IPclNPlugin entry with a parameterless constructor.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol type ||
            type.TypeKind != TypeKind.Class ||
            type.IsStatic)
        {
            return;
        }

        if (!ImplementsIPclNPlugin(type))
            return;

        bool valid =
            type.DeclaredAccessibility == Accessibility.Public &&
            !type.IsAbstract &&
            !type.IsGenericType &&
            type.TypeParameters.Length == 0 &&
            type.InstanceConstructors.Any(ctor =>
                ctor.Parameters.Length == 0 &&
                ctor.DeclaredAccessibility == Accessibility.Public &&
                !ctor.IsStatic);

        if (valid)
            return;

        Location location = type.Locations.FirstOrDefault(static loc => loc.IsInSource) ?? Location.None;
        context.ReportDiagnostic(Diagnostic.Create(Rule, location, type.ToDisplayString()));
    }

    private static bool ImplementsIPclNPlugin(INamedTypeSymbol type)
    {
        foreach (INamedTypeSymbol iface in type.AllInterfaces)
        {
            if (iface.Name == "IPclNPlugin" &&
                iface.ContainingNamespace.ToDisplayString() == "PCL.N.Plugin")
            {
                return true;
            }
        }

        return false;
    }
}
