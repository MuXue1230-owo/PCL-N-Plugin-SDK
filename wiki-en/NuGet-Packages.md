# NuGet Packages

> Applies to PCL N Plugin SDK 0.2.1.

This page is the English counterpart of the matching Chinese SDK guide. It documents the same contracts, examples, and compatibility requirements.

## SDK 0.2.1 requirements

- Target .NET 10 and reference only public PCLN.Plugin packages.
- Reference `PCLN.Plugin.i18n` in every plugin and use `PclLocalizedString` for every user-visible UI or settings-page string.
- Provide both locales/zh-CN.json and locales/en-US.json through the localization resource path. Both files must contain exactly the same key set.
- Declare required and optional permissions explicitly and degrade gracefully when optional capabilities are unavailable.
- Keep the package manifest icon separate from the PNG, JPEG, or WebP market icon uploaded in the publisher workbench.
- Submit Chinese and English market name, summary, and description before review or publishing.

## Internationalization package

`PCLN.Plugin.i18n` exposes `PclLocalizedString`, localized settings-page capabilities, and helpers for the host-provided `IPluginLocalizationService`. Construct visible text with a stable resource key and a Simplified Chinese fallback:

```csharp
new PclLocalizedString("settings.title", "设置")
```

`PclUiString`, `PluginSettingsPageDescriptor`, and the legacy settings-page capabilities are obsolete compatibility adapters. New plugins must use the i18n contracts.

## Contract parity

JSON, C#, AXAML, signing, Testing Host, and publishing behavior are identical across languages. See the SDK source schemas and examples for copy-ready contracts.
