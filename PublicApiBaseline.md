# Public API baseline — 0.1.0-alpha.3

This alpha baseline protects the public ABI from accidental changes. Intentional alpha API changes must update this file and release notes.

## Entry and lifecycle

- `PCL.N.Plugin.IPclNPlugin`
- `PCL.N.Plugin.IPluginContext`
- `PCL.N.Plugin.IPluginLifetime`
- `PCL.N.Plugin.IPluginRegistration`

## Identity and versions

- `PluginId`
- `PluginVersion`
- `PluginApiVersion`
- `PluginApiVersionRange`
- `PluginDescriptor`

## Services

- `IPluginServiceProvider`
- `IPluginLogger`
- `IPluginDispatcher`
- `IPluginNotificationService`
- `IPluginSettingsStore`
- `IPluginCommandService`
- `IPluginTaskService`
- `IPluginInstanceReadService`

## Manifest and UI

- `PluginManifest`
- `PluginUiManifest`
- `PluginUiTargetManifest`
- `PluginUiOperationManifest`
- `IPluginUiSurfaceRegistry`
- `IPluginUiPatchService`

No type from `PCL.Application`, `PCL.Desktop`, or private `PCL.Plugin` is part of this baseline.
