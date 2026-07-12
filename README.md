# PCL N Plugin SDK

Public contracts, documentation, and samples for extending PCL N.

## Status

The SDK ABI is being designed. No 0.x package published from this repository should be treated as stable yet.

Implemented in the first platform phase:

- `PCL.N.Plugin.Abstractions` lifecycle and capability ABI
- stable service IDs: logging / dispatcher / notifications / settings / commands / tasks
- strict plugin IDs and SemVer 2.0 versions
- typed `plugin.json` manifest contracts
- manifest/API/dependency/permission validation
- `schemas/plugin.schema.json`
- `PCL.N.Plugin.Analyzers` **PNPSDK001–003** (forbid host assembly refs), **PNPSDK004** (entry type shape), **PNPSDK010** (untracked background work)
- UI Surface contracts (`IPluginUiSurfaceRegistry`, slots, inject contribution capability)
- three-platform build, test, and pack CI

The first-party `PCL.Plugin` HostModule is deliberately not part of this public third-party ABI. Third-party plugins implement `IPclNPlugin`; the built-in HostModule uses a separate host contract owned by PCL N.

## Local validation

Install the .NET 10 SDK, then run:

```console
dotnet test PCL-N-Plugin-SDK.slnx
```

The solution includes `PCL.N.Plugin.Abstractions`, developer helpers, an in-memory test host, a minimal `IPclNPlugin` example, and SDK unit tests.

## Planned scope

- Additional capability-based services without launcher-internal access
- `.pnp` reproducible build and OpenPGP signing targets
- Declarative settings-page registration
- Compatibility metadata and diagnostics
- Minimal sample plugin and build templates

PCL N and this SDK are licensed under the Apache License 2.0 unless a file states otherwise.
