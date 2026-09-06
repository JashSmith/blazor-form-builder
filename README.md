# Blazor Form & Page Builder

A plugin-first, low-code page and form designer built with standalone Blazor WebAssembly and .NET 10. This repository is the foundation for a larger form and BPMN process-management platform.

## Builder workspace

The application now starts in a guided builder workspace with four tools:

- **Page builder** manages multiple pages, responsive grids, and layout templates.
- **Header builder** creates reusable localized brands and navigation menus.
- **Footer builder** creates reusable localized links and status widgets.
- **Form builder** creates runnable forms with drag-and-drop fields and plugin-owned validation.
- **Workflow builder** connects user tasks to immutable published form versions.

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
| `BlazorFormBuilder.App` | WASM client with server-first and local fallback stores |
| `BlazorFormBuilder.Api` | ASP.NET Core host and versioned JSON persistence API |

The dependency direction keeps field packages replaceable: the host discovers `IFormFieldPlugin` registrations and the designer renders their preview components dynamically.

## Run locally

Install the .NET 10 SDK, then run:

```bash
dotnet restore BlazorFormBuilder.slnx
dotnet run --project src/BlazorFormBuilder.Api
```

Open the HTTPS URL printed by ASP.NET Core, select **Create workspace**, and enter an organization name, owner email, and password of at least eight characters. Later sign-ins use the generated tenant slug (for example, `Acme Portal` becomes `acme-portal`).

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

The hosted app starts with a sign-in/create-workspace/join screen. The first account creates a tenant and becomes its owner. Authentication uses a protected HttpOnly cookie; passwords are stored as one-way hashes, and data-protection keys persist across restarts.

Owners can open **Team**, invite a member as Editor or Viewer, and copy the generated one-time token. The invited member selects **Join**, pastes that token, and chooses a password. Tokens are stored only as hashes, expire after seven days, and cannot be reused.

| Role | View drafts | Edit and save | Invite members |
| --- | --- | --- | --- |
| Owner | Yes | Yes | Yes |
| Editor | Yes | Yes | No |
| Viewer | Yes | No | No |

Authenticated Workspace and Form requests are scoped from the tenant claim—not a client-supplied tenant id—then saved through `/api/workspaces` and `/api/forms`. Responses carry an `ETag`, and stale updates receive `409 Conflict` instead of overwriting a newer edit. Browser snapshots are also namespaced by tenant id, so accounts sharing a browser cannot load each other's local fallback.

### Publishing and workflow binding

`Save form` updates the editable draft. `Publish version` creates a detached, immutable snapshot numbered independently per form. Existing publications are never overwritten when the draft changes.

The Workflow builder provides a first executable-domain slice for BPMN-style user tasks:

- add ordered user tasks between Start and End;
- select a published form and exact version for each task;
- persist the workflow with optimistic concurrency;
- reject missing or cross-tenant publication references on the API;
- allow Viewers to inspect workflows while restricting changes to Owners and Editors.

## Next slice

Add BPMN gateways and transitions, then create process instances that execute the version-locked user tasks.
