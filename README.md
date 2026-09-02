# Blazor Form & Page Builder

A plugin-first, low-code page and form designer built with standalone Blazor WebAssembly and .NET 10. This repository is the foundation for a larger form and BPMN process-management platform.

## Builder workspace

The application now starts in a working builder workspace with two tools:

- **Page builder** manages multiple pages, responsive grids, layout templates and page chrome.
- **Form builder** creates runnable forms with drag-and-drop fields and plugin-owned validation.

Every draggable toolbox item also supports click-to-add for touch devices and accessibility.

### Page builder capabilities

- create and switch between multiple pages;
- apply Blank, Landing, Dashboard, or Sidebar skeletons;
- drag Content, Hero, Form, Sidebar, Cards, and Empty boxes onto the canvas;
- reorder boxes and configure their responsive column spans;
- independently set Desktop (1–24), Tablet (1–16), and Mobile (1–8) grid columns;
- switch the canvas between 100%, 768px, and 390px viewport previews;
- build a header with editable brand and menu items;
- build a footer with links and live Messages, Logs, Progress, Clock, and Connection widgets;
- save and restore the complete workspace in browser storage.

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

Open **Form builder**, drag fields from the toolbox onto the canvas, reorder them by dragging, configure them in the property panel, save the valid definition, then use **Preview form** to enter values and exercise each plugin's runtime validation.

Drafts are currently stored in the browser's `localStorage` behind the `IFormDefinitionStore` abstraction. A server implementation can replace it without changing the designer RCL.

## Next slice

Persist versioned form definitions through an ASP.NET Core API, add authentication and optimistic concurrency, then connect published forms to BPMN user tasks.
