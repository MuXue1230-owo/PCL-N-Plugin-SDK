# Public API baseline — 0.1.2

This baseline protects the public ABI from accidental changes. Intentional API changes must update this file and release notes.

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
- `IPluginLocalizationService`
- `IPluginSecureStorage`
- `IPluginUriLauncher`
- `IPluginBackgroundTaskService`
- `IPluginBackgroundTask`
- `PluginBackgroundTaskProgress`
- `PluginBackgroundTaskStep`
- `PluginBackgroundTaskStepState`
- `PluginServiceIds.BackgroundTasks`
- `PluginSecretKey`
- `PluginSecretReadResult`
- `PluginSecretOperationResult`
- `PluginSecureStorageStatus`
- `IPluginExportRegistry`
- `IPluginDataMigration`
- `IPluginMigrationContext`
- `IPluginHealthCheck`
- `IPluginMarketClient`
- `PluginMarketPackageVerificationRequest`
- `PluginMarketPackageVerification`

## Manifest and UI

- `PluginManifest`
- `PluginUiManifest`
- `PluginUiTargetManifest`
- `PluginUiOperationManifest`
- `IPluginUiSurfaceRegistry`
- `IPluginUiPatchService`
- `UiTargetId`
- `PluginPageDescriptor`
- `IPluginNavigationService`
- `IAvaloniaUiAccessService`
- `IAvaloniaUiContext`
- `IUiTargetHandle`
- `IAvaloniaPluginPageService`
- `IAvaloniaPluginWindowService`

No type from `PCL.Application`, `PCL.Desktop`, or private `PCL.Plugin` is part of this baseline.
