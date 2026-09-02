# Blazor Form Builder

A plugin-first, low-code form designer built with standalone Blazor WebAssembly and .NET 10. This repository is the foundation for a larger form and BPMN process-management platform.

## First vertical slice

The form-designer MVP provides a working design-to-runtime flow:

- field toolbox populated through dependency-injected RCL plugins;
- canvas with selection, removal, and field reordering;
- live property editing for label, key, placeholder, and required state;
- JSON definition preview and schema validation;
- browser-local draft persistence and automatic restore;
- interactive runtime preview with submission validation;
- standard text, email, number, date, long-text, and checkbox plugins;
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

## Current workflow

Create fields in the toolbox, configure them in the property panel, save the valid definition, then use **Preview form** to enter values and exercise each plugin's runtime validation.

Drafts are currently stored in the browser's `localStorage` behind the `IFormDefinitionStore` abstraction. A server implementation can replace it without changing the designer RCL.

## Next slice

Persist versioned form definitions through an ASP.NET Core API, add authentication and optimistic concurrency, then connect published forms to BPMN user tasks.
