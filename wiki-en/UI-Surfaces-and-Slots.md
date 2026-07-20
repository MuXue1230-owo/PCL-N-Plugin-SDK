# UI Surfaces and Slots

> Applies to PCL N Plugin SDK 0.2.4.

This page is the English counterpart of the matching Chinese SDK guide. It documents the same contracts, examples, and compatibility requirements.

## SDK 0.2.4 requirements

- Target .NET 10 and reference only public PCLN.Plugin packages.
- Provide both locales/zh-CN.json and locales/en-US.json through the localization resource path.
- Declare required and optional permissions explicitly and degrade gracefully when optional capabilities are unavailable.
- Keep the package manifest icon separate from the PNG, JPEG, or WebP market icon uploaded in the publisher workbench.
- Submit Chinese and English market name, summary, and description before review or publishing.

## Launch page slots (`pcl.page.launch` 3.2)

| Slot | Status | Behavior |
|---|---|---|
| `cards.flip` | **Preferred** | Each inject becomes one full-bleed, swipeable registered card |
| `primary-actions.after` | **Deprecated** compatibility shell | Injects into a single stacked compat card; keep working for older plugins |
| `primary-actions.launch-button` | Stable | Observe / modify / wrap the primary launch button |

New plugins should target `cards.flip` with surface range `>=3.2 <4.0`.

## Contract parity

JSON, C#, AXAML, signing, Testing Host, and publishing behavior are identical across languages. See the SDK source schemas and examples for copy-ready contracts.
