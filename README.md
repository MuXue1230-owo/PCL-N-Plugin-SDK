# PCL N Plugin SDK

Public contracts, documentation, and samples for extending PCL N.

## Status

The SDK ABI is being designed. No 0.x package published from this repository should be treated as stable yet.

The first-party `PCL.Plugin` HostModule is deliberately not part of this public third-party ABI. Third-party plugins implement `IPclNPlugin`; the built-in HostModule uses a separate host contract owned by PCL N.

## Local validation

Install the .NET 10 SDK, then run:

```console
dotnet test PCL-N-Plugin-SDK.slnx
```

The solution includes `PCL.N.Plugin.Abstractions`, developer helpers, an in-memory test host, a minimal `IPclNPlugin` example, and SDK unit tests.

## Planned scope

- Versioned HostModule and host-builder contracts
- Capability-based services instead of access to launcher internals
- Declarative settings-page registration
- Compatibility metadata and diagnostics
- Minimal sample plugin and build templates

PCL N and this SDK are licensed under the Apache License 2.0 unless a file states otherwise.
