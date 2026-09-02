# Blazor Form Builder

A plugin-first, low-code form designer built with standalone Blazor WebAssembly and .NET 10. This repository is the foundation for a larger form and BPMN process-management platform.

## First vertical slice

The initial feature provides a working form-designer shell:

- field toolbox populated through dependency-injected RCL plugins;
- canvas with selection, removal, and field reordering;
- live property editing for label, key, placeholder, and required state;
- JSON definition preview;
- a standard text-input plugin as the reference implementation;
- domain tests and GitHub Actions CI.

## Architecture

| Project | Responsibility |
| --- | --- |
| `BlazorFormBuilder.Core` | UI-independent form domain and operations |
| `BlazorFormBuilder.Abstractions` | Stable plugin contract |
| `BlazorFormBuilder.Components` | Reusable designer RCL |
| `BlazorFormBuilder.Plugins.Standard` | Built-in fields packaged as an RCL plugin |
| `BlazorFormBuilder.App` | Standalone WASM composition root |

The dependency direction keeps field packages replaceable: the host discovers `IFormFieldPlugin` registrations and the designer renders their preview components dynamically.

## Run locally

Install the .NET 10 SDK, then run:

```bash
dotnet restore BlazorFormBuilder.slnx
dotnet run --project src/BlazorFormBuilder.App
```

Run tests with:

```bash
dotnet test BlazorFormBuilder.slnx
```

## Git Flow

- `main`: production-ready history
- `develop`: integration branch
- `feature/<name>`: feature work branched from `develop`
- `release/<version>`: stabilization
- `hotfix/<name>`: urgent production fixes

After this repository bootstrap, create `develop` from `main` and open subsequent feature branches against `develop`.

## Next slice

Persist versioned form definitions behind an API boundary, add validation rules, and introduce additional field plugins before connecting form tasks to the BPMN model.
