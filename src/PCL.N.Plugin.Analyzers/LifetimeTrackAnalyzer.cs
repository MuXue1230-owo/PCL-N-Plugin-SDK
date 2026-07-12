using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PCL.N.Plugin.Analyzers;

/// <summary>
/// PNPSDK005: warn when Register/Contribute results from known plugin APIs are discarded
/// without assignment or Lifetime.Track (design §8).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LifetimeTrackAnalyzer : DiagnosticAnalyzer
{
    public const string RuleId = "PNPSDK005";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        "Plugin registration may not be tracked",
        "Result of '{0}' should be stored or passed to IPluginLifetime.Track so unload can release it",
        category: "PCL.N.Plugin.Lifecycle",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Discarded IPluginRegistration instances leak host registrations across unload.");

    private static readonly HashSet<string> RegistrationMethods = new(StringComparer.Ordinal)
    {
        "Register",
        "Contribute",
        "Run",
        "SchedulePeriodic",
        "Export"
    };

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation ||
            invocation.TargetMethod is null)
        {
            return;
        }

        IMethodSymbol method = invocation.TargetMethod;
        if (!RegistrationMethods.Contains(method.Name))
            return;

        if (!ReturnsRegistration(method.ReturnType))
            return;

        // Used as expression statement: foo.Register(...);
        if (invocation.Parent is IExpressionStatementOperation)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                Rule,
                invocation.Syntax.GetLocation(),
                method.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }
    }

    private static bool ReturnsRegistration(ITypeSymbol? type)
    {
        if (type is null)
            return false;
        if (type.Name is "IPluginRegistration" or "IPluginTaskRegistration")
            return type.ContainingNamespace?.ToDisplayString() == "PCL.N.Plugin";
        return type.AllInterfaces.Any(static i =>
            i.Name is "IPluginRegistration" or "IPluginTaskRegistration" &&
            i.ContainingNamespace?.ToDisplayString() == "PCL.N.Plugin");
    }
}
