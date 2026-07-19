using System.Globalization;

namespace PCL.N.Plugin;

/// <summary>A plugin-owned resource key with a Simplified Chinese fallback.</summary>
public record PclLocalizedString
{
    protected PclLocalizedString(string fallback, string? key, bool allowMissingKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fallback);
        if (!allowMissingKey && string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Localization key cannot be empty.", nameof(key));
        Fallback = fallback;
        Key = string.IsNullOrWhiteSpace(key) ? null : key.Trim();
    }

    public PclLocalizedString(string key, string fallback)
        : this(fallback, key, allowMissingKey: false)
    {
    }

    public string? Key { get; init; }

    public string Fallback { get; init; }

    /// <summary>Formatting arguments resolved with the plugin's active culture.</summary>
    public IReadOnlyList<object?> Arguments { get; init; } = [];

    public static PclLocalizedString Create(string key, string fallback) => new(key, fallback);

    /// <summary>Returns an immutable formatted copy of this localized string.</summary>
    public PclLocalizedString Format(params object?[] arguments) =>
        this with { Arguments = arguments?.ToArray() ?? [] };
}

/// <summary>Helpers that resolve strongly typed plugin text through the host localization service.</summary>
public static class PclPluginI18n
{
    public const string SimplifiedChinese = "zh-CN";
    public const string AmericanEnglish = "en-US";

    public static string GetString(this IPluginLocalizationService service, PclLocalizedString text)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(text);
        if (text.Arguments.Count > 0)
            return service.FormatString(text, [.. text.Arguments]);
        return text.Key is null ? text.Fallback : service.GetString(text.Key, text.Fallback);
    }

    public static string FormatString(
        this IPluginLocalizationService service,
        PclLocalizedString text,
        params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(text);
        return text.Key is null
            ? string.Format(service.GetCurrentCulture(), text.Fallback, arguments)
            : service.FormatString(text.Key, text.Fallback, arguments);
    }

    public static bool SupportsRequiredCultures(this IPluginLocalizationService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        return service.SupportedCultures.Contains(SimplifiedChinese, StringComparer.OrdinalIgnoreCase) &&
               service.SupportedCultures.Contains(AmericanEnglish, StringComparer.OrdinalIgnoreCase);
    }

    public static CultureInfo GetCurrentCulture(this IPluginLocalizationService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        try
        {
            return CultureInfo.GetCultureInfo(service.CurrentCulture);
        }
        catch (CultureNotFoundException)
        {
            return CultureInfo.GetCultureInfo(service.DefaultCulture);
        }
    }
}
