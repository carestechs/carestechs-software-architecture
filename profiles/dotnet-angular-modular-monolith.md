# Stack Profile: .NET + Angular Modular Monolith

**Status:** Active
**Assumes:** .NET 10+, Angular 20+, PostgreSQL, EF Core, Tailwind CSS 4+

## Overview

A curated set of ADRs for building a modular monolith backend with an Angular SPA frontend. ADRs are categorized by how essential they are to the stack's coherence.

---

## Solution Structure

```
MyApp/
├── MyApp.sln
│
├── src/
│   ├── MyApp.Api/                          # Thin API host (composition root)
│   │   ├── Program.cs                      # DI registration, middleware, pipeline
│   │   ├── appsettings.json
│   │   └── MyApp.Api.csproj                # References all module projects
│   │
│   ├── MyApp.Contracts/                    # Shared interfaces and DTOs for cross-module communication
│   │   ├── ICatalogService.cs
│   │   ├── IIdentityService.cs
│   │   └── MyApp.Contracts.csproj
│   │
│   ├── MyApp.Modules.Catalog/             # Example feature module
│   │   ├── Controllers/
│   │   │   └── CatalogController.cs
│   │   ├── Services/
│   │   │   ├── ICatalogService.cs
│   │   │   └── CatalogService.cs
│   │   ├── Entities/
│   │   │   └── Product.cs
│   │   ├── DTOs/
│   │   │   ├── ProductDto.cs
│   │   │   └── CreateProductRequest.cs
│   │   ├── CatalogDbContext.cs
│   │   ├── CatalogModuleExtensions.cs      # AddCatalogModule()
│   │   └── MyApp.Modules.Catalog.csproj
│   │
│   └── MyApp.Modules.Identity/            # Another feature module (same structure)
│       ├── Controllers/
│       ├── Services/
│       ├── Entities/
│       ├── DTOs/
│       ├── IdentityDbContext.cs
│       ├── IdentityModuleExtensions.cs     # AddIdentityModule()
│       └── MyApp.Modules.Identity.csproj
│
├── client/                                 # Angular SPA
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                       # Singleton services, guards, interceptors
│   │   │   ├── shared/                     # Reusable standalone components, pipes, directives
│   │   │   ├── features/                   # Feature-based route folders
│   │   │   │   ├── catalog/
│   │   │   │   │   ├── catalog.routes.ts
│   │   │   │   │   ├── catalog-list.component.ts
│   │   │   │   │   ├── catalog-list.component.html
│   │   │   │   │   ├── catalog-detail.component.ts
│   │   │   │   │   └── catalog-detail.component.html
│   │   │   │   └── auth/
│   │   │   │       ├── auth.routes.ts
│   │   │   │       ├── login.component.ts
│   │   │   │       └── login.component.html
│   │   │   ├── app.component.ts
│   │   │   ├── app.component.html
│   │   │   ├── app.config.ts
│   │   │   └── app.routes.ts
│   │   ├── styles.css                      # Global Tailwind imports only
│   │   └── index.html
│   ├── tailwind.config.js
│   ├── angular.json
│   └── package.json
│
└── tests/
    ├── MyApp.Modules.Catalog.Tests/
    └── MyApp.Modules.Identity.Tests/
```

---

## Required (core to this stack — do not omit)

These ADRs define the fundamental architecture. Removing any of them breaks the coherence of the stack.

| ADR | Summary | Depends On |
|-----|---------|------------|
| `adrs/dotnet/modular-monolith.md` | Single deployable, feature modules as separate .csproj with clear boundaries | — |
| `adrs/dotnet/dbcontext-per-module.md` | Each module owns its own DbContext. Migrations are per-module. | `modular-monolith` |
| `adrs/dotnet/cross-module-by-id.md` | Modules reference each other by ID only. No cross-module navigation properties. | `modular-monolith`, `dbcontext-per-module` |
| `adrs/dotnet/thin-api-host.md` | API host is composition root only — no controllers, services, or business logic. | `modular-monolith` |
| `adrs/dotnet/service-layer-logic.md` | Controllers are thin. All business logic lives in service classes. | — |
| `adrs/dotnet/dto-at-boundary.md` | Never expose EF entities via API. Mapping happens in service layer. | `service-layer-logic` |
| `adrs/dotnet/async-all-the-way.md` | All I/O uses async/await. Async suffix on service methods. | — |
| `adrs/angular/standalone-components.md` | All components standalone. No NgModules. | — |

## Recommended (strong defaults — can be swapped with noted alternatives)

These are battle-tested defaults. You can swap them, but you should have a good reason.

| ADR | Summary | Alternative |
|-----|---------|-------------|
| `adrs/dotnet/rfc7807-errors.md` | RFC 7807 Problem Details for all errors. Global exception handler. | Custom error envelope (not recommended) |
| `adrs/database/uuid-primary-keys.md` | All PKs are UUIDs. No auto-increment. | Auto-increment integers (simpler but less secure) |
| `adrs/database/snake-case-naming.md` | snake_case tables/columns via EF Core naming convention. | PascalCase with quoting (non-idiomatic for PostgreSQL) |
| `adrs/database/timestamptz-always.md` | All datetimes are timestamptz. C# uses DateTimeOffset. | timestamp without timezone (loses timezone context) |
| `adrs/api/rest-envelope.md` | All responses wrapped in `{ data, meta }` envelope. | Flat responses with pagination in headers |
| `adrs/api/jwt-bearer-auth.md` | JWT Bearer tokens. Short-lived access + rotated refresh. | Session cookies (if not SPA architecture) |
| `adrs/angular/separate-template-file.md` | Component templates in separate `.html` files via `templateUrl`. No inline templates. | Inline `template` strings (loses HTML tooling and readability) |
| `adrs/angular/signals-state.md` | Angular Signals for reactive state. RxJS only for HTTP/async. | RxJS BehaviorSubjects (more boilerplate) |
| `adrs/angular/tailwind-no-css.md` | Tailwind utility classes only. No component CSS files. | Component-scoped SCSS (if team prefers) |

## Optional (pick based on project needs)

These address specific concerns that not every project has.

| ADR | Summary | When to Include |
|-----|---------|-----------------|
| `adrs/database/soft-deletes.md` | Soft deletion via nullable `deleted_at` column. | Projects needing audit trails or undo capability |
| `adrs/api/offset-pagination.md` | Offset pagination with page/pageSize/sortBy/sortDir. Requires `rest-envelope`. | Any project with list endpoints |

---

## Key Cross-Cutting Concerns

When using this stack, these patterns emerge from the combination of ADRs:

- **Naming translation:** C# PascalCase properties automatically map to snake_case database columns and camelCase JSON (via System.Text.Json default policy)
- **Time handling:** Backend stores UTC DateTimeOffset, database uses timestamptz, frontend converts to local display time
- **ID strategy:** UUIDs flow end-to-end: generated in C#, stored as uuid in PostgreSQL, serialized as strings in JSON
- **Auth flow:** Angular app stores JWT in memory or httpOnly cookie, sends via Authorization header, .NET validates with `[Authorize]`
- **Module isolation:** Each module is a .csproj with its own DbContext, controllers, services, and DTOs. Cross-module communication is by ID + shared interface only.

## Development Workflow

- **Local development first:** Set up local development immediately after the base projects have minimal setup (solution structure, project references, empty DbContexts, and module registration wired in `Program.cs`). The application must build, run, and be locally testable before adding any feature code. This ensures a fast feedback loop and catches configuration issues early — never defer local dev setup to "later".
