# PCL N Plugin SDK

Public contracts, documentation, and samples for extending PCL N.

## Status

The SDK ABI is being designed. No package published from this repository should be treated as stable yet.

The current first-party `PCL.Plugin` integration compiles against the host contracts on the PCL N `dev` branch. This keeps the initial HostModule integration testable while the stable, versioned SDK boundary is finalized.

## Planned scope

- Versioned HostModule and host-builder contracts
- Capability-based services instead of access to launcher internals
- Declarative settings-page registration
- Compatibility metadata and diagnostics
- Minimal sample plugin and build templates

PCL N and this SDK are licensed under the Apache License 2.0 unless a file states otherwise.
