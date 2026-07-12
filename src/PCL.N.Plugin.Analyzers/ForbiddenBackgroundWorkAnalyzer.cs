using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace PCL.N.Plugin.Analyzers;

/// <summary>
/// PNPSDK010: plugins must not create untracked Thread / Timer / Task.Run / FileSystemWatcher
/// work; use <c>IPluginTaskService</c> instead (design §9.5).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForbiddenBackgroundWorkAnalyzer : DiagnosticAnalyzer
{
    public const string RuleId = "PNPSDK010";

    private static readonly DiagnosticDescriptor Rule = new(
        RuleId,
        "Untracked background work is forbidden",
        "Do not use '{0}' in plugins; register work through IPluginTaskService (pcl.tasks) so unload can cancel it",
        category: "PCL.N.Plugin.Lifecycle",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Untracked threads, timers, Task.Run, and file watchers leak across plugin unload.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(AnalyzeInvocation, OperationKind.Invocation);
        context.RegisterOperationAction(AnalyzeObjectCreation, OperationKind.ObjectCreation);
    }

    private static void AnalyzeInvocation(OperationAnalysisContext context)
    {
        if (context.Operation is not IInvocationOperation invocation ||
            invocation.TargetMethod is null)
        {
            return;
        }

        IMethodSymbol method = invocation.TargetMethod;
        string? typeName = method.ContainingType?.ToDisplayString();
        if (typeName is null)
            return;

        if (typeName is "System.Threading.Tasks.Task" or "System.Threading.Tasks.Task`1" &&
            method.Name is "Run" or "Factory")
        {
            Report(context, invocation.Syntax, "Task.Run");
            return;
        }

        if (typeName == "System.Threading.Thread" && method.Name is "Start" or ".ctor")
            Report(context, invocation.Syntax, "Thread");
    }

    private static void AnalyzeObjectCreation(OperationAnalysisContext context)
    {
        if (context.Operation is not IObjectCreationOperation creation ||
            creation.Type is null)
        {
            return;
        }

        string typeName = creation.Type.ToDisplayString();
        if (typeName is "System.Threading.Thread" or
            "System.Threading.Timer" or
            "System.Timers.Timer" or
            "System.IO.FileSystemWatcher")
        {
            Report(context, creation.Syntax, creation.Type.Name);
        }
    }

    private static void Report(OperationAnalysisContext context, SyntaxNode syntax, string name)
    {
        context.ReportDiagnostic(Diagnostic.Create(Rule, syntax.GetLocation(), name));
    }
}
