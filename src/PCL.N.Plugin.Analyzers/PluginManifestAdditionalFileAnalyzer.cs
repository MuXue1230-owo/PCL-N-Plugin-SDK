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

    public const string SigningPolicyRuleId = "PNPSDK011";

    private static readonly DiagnosticDescriptor ManifestRule = Rule(ManifestRuleId, "Plugin manifest is invalid", "plugin.json is missing required field '{0}'");
    private static readonly DiagnosticDescriptor ApiRule = Rule(ApiRuleId, "Plugin API range is required", "plugin.json must declare api.minimum and api.maximumExclusive");
    private static readonly DiagnosticDescriptor UiRule = Rule(UiRuleId, "AXAML UI resource is not declared correctly", "AXAML path '{0}' must be under ui/ and end with .axaml");
    private static readonly DiagnosticDescriptor SigningPolicyRule = Rule(SigningPolicyRuleId, "Plugin signing policy is inconsistent", "plugin.json signing policy is inconsistent: {0}");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(ManifestRule, ApiRule, UiRule, SigningPolicyRule);

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
        AnalyzeSigningPolicy(text, context);
    }

    private static void AnalyzeSigningPolicy(string text, CompilationAnalysisContext context)
    {
        HashSet<string> fingerprints = ExtractFingerprints(text, "fingerprint");
        foreach (string fingerprint in ExtractFingerprints(text, "fingerprints"))
            fingerprints.Add(fingerprint);
        foreach (string revoked in ExtractFingerprints(text, "revokedFingerprints"))
        {
            if (fingerprints.Contains(revoked))
                context.ReportDiagnostic(Diagnostic.Create(SigningPolicyRule, Location.None, "revoked fingerprint is still declared as an active signing key"));
        }

        Match minimum = Regex.Match(text, "\\\"minimumValidSignatures\\\"\\s*:\\s*(?<count>[0-9]+)", RegexOptions.CultureInvariant);
        if (minimum.Success && int.TryParse(minimum.Groups["count"].Value, out int count) && count > Math.Max(1, fingerprints.Count))
            context.ReportDiagnostic(Diagnostic.Create(SigningPolicyRule, Location.None, "minimumValidSignatures exceeds the declared signing key count"));
    }

    private static HashSet<string> ExtractFingerprints(string text, string propertyName)
    {
        HashSet<string> result = new(StringComparer.OrdinalIgnoreCase);
        foreach (Match match in Regex.Matches(text, "\\\"" + Regex.Escape(propertyName) + "\\\"\\s*:\\s*\\\"(?<value>[0-9A-Fa-f]{40,64})\\\"", RegexOptions.CultureInvariant))
            result.Add(match.Groups["value"].Value.ToUpperInvariant());
        Match array = Regex.Match(text, "\\\"" + Regex.Escape(propertyName) + "\\\"\\s*:\\s*\\[(?<values>[^\\]]*)\\]", RegexOptions.Singleline | RegexOptions.CultureInvariant);
        if (array.Success)
        {
            foreach (Match item in Regex.Matches(array.Groups["values"].Value, "\\\"(?<value>[0-9A-Fa-f]{40,64})\\\"", RegexOptions.CultureInvariant))
                result.Add(item.Groups["value"].Value.ToUpperInvariant());
        }
        return result;
    }

    private static DiagnosticDescriptor Rule(string id, string title, string message) => new(
        id, title, message, "PCL.N.Plugin.Manifest", DiagnosticSeverity.Error, true);
}
