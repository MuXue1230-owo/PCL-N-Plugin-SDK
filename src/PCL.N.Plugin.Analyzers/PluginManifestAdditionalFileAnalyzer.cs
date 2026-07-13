using System.Collections.Immutable;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace PCL.N.Plugin.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PluginManifestAdditionalFileAnalyzer : DiagnosticAnalyzer
{
    public const string ManifestRuleId = "PNPSDK007";
    public const string ApiRuleId = "PNPSDK008";
    public const string UiRuleId = "PNPSDK009";

    private static readonly DiagnosticDescriptor ManifestRule = Rule(ManifestRuleId, "Plugin manifest is invalid", "plugin.json is missing required field '{0}'");
    private static readonly DiagnosticDescriptor ApiRule = Rule(ApiRuleId, "Plugin API range is required", "plugin.json must declare api.minimum and api.maximumExclusive");
    private static readonly DiagnosticDescriptor UiRule = Rule(UiRuleId, "AXAML UI resource is not declared correctly", "AXAML path '{0}' must be under ui/ and end with .axaml");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(ManifestRule, ApiRule, UiRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(Analyze);
    }

    private static void Analyze(CompilationAnalysisContext context)
    {
        AdditionalText? manifest = context.Options.AdditionalFiles.FirstOrDefault(static file =>
            string.Equals(Path.GetFileName(file.Path), "plugin.json", StringComparison.OrdinalIgnoreCase));
        if (manifest?.GetText(context.CancellationToken) is not { } source)
            return;
        string text = source.ToString();
        foreach (string field in new[] { "formatVersion", "manifestVersion", "id", "name", "version", "publisher", "entryPoint", "host", "signing" })
        {
            if (!Regex.IsMatch(text, "\\\"" + Regex.Escape(field) + "\\\"\\s*:", RegexOptions.CultureInvariant))
                context.ReportDiagnostic(Diagnostic.Create(ManifestRule, Location.None, field));
        }
        if (!Regex.IsMatch(text, "\\\"api\\\"\\s*:\\s*\\{[^}]*\\\"minimum\\\"\\s*:[^}]*\\\"maximumExclusive\\\"\\s*:", RegexOptions.Singleline | RegexOptions.CultureInvariant))
            context.ReportDiagnostic(Diagnostic.Create(ApiRule, Location.None));
        foreach (Match match in Regex.Matches(text, "\\\"axaml\\\"\\s*:\\s*\\\"(?<path>[^\\\"]+)\\\"", RegexOptions.CultureInvariant))
        {
            string path = match.Groups["path"].Value;
            if (!path.StartsWith("ui/", StringComparison.Ordinal) || !path.EndsWith(".axaml", StringComparison.OrdinalIgnoreCase) || path.Contains(".."))
                context.ReportDiagnostic(Diagnostic.Create(UiRule, Location.None, path));
        }
    }

    private static DiagnosticDescriptor Rule(string id, string title, string message) => new(
        id, title, message, "PCL.N.Plugin.Manifest", DiagnosticSeverity.Error, true);
}
