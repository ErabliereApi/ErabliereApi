# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ErabliereApi is a maple-syrup-production (érablière) monitoring platform: an ASP.NET Core REST API plus an Angular web app, collecting sensor data (temperature, vacuum, tank level), alerts, reports, notes, and AI chat features. Much of the codebase — identifiers, routes, comments, docs — is in **French**; follow that convention when adding code.

Project tracking lives in Azure DevOps: https://dev.azure.com/freddycoder/ErabliereAPI

## Solution layout

- `ErabliereApi/` — the ASP.NET Core web API (main project, targets .NET 9; `global.json` pins SDK 10 with `rollForward: latestMajor`).
- `ErabliereModel/` — data-model project (`ErabliereApi.Donnees`), one entity per file (Erabliere, Capteur, DonneeCapteur, Alerte, ...). `Erabliere` is the root of the data hierarchy; most entities are owned by one.
- `ErabliereIU/` — Angular 22 front-end. **It has its own `ErabliereIU/CLAUDE.md`** — read it before working there (dev server, Cypress tests, runtime config, MSAL auth).
- `ErabliereAPI.Proxy/` — NSwag-generated C# client published to NuGet. Regenerated with NSwag Studio from the OpenAPI spec + `GenerateProxy.ps1` (see its Readme); don't hand-edit generated files.
- `ErabliereApi.Test/` (unit), `ErabliereApi.Integration.Test/` (in-memory `WebApplicationFactory` + AngleSharp; includes Stripe webhook JSON fixtures), `ErabliereApi.Test.Autofixture/` — all xUnit.
- `Infrastructure/`, `docker-compose*.yaml`, `Dockerfile` — Kubernetes/docker deployment; `PythonScripts/` — device/data-feeding scripts; `Postman/` — API collections.

## Commands

Backend (from repo root):
- `dotnet build ErabliereApi.sln`
- `dotnet test` — all test projects. Single project: `dotnet test ErabliereApi.Test`. Single test: `dotnet test --filter "FullyQualifiedName~MyTestName"`.
- `.\start-code-coverage-report.ps1` — tests with coverage + HTML report in `coveragereport/` (needs `reportgenerator` tool).
- Run the API alone: `dotnet watch run` in `ErabliereApi/` (Development uses HTTP 5000 / HTTPS 5001).

Full stack:
- `.\start-light.ps1` — starts the API (`dotnet watch`) and the Angular dev server (`npm start`, https://localhost:4200). Pass `-startStripe $true` to also forward Stripe webhooks.
- `.\start-local-debug-services.ps1` — same plus Stripe CLI login/listen and optional sibling repos (EmailImagesObserver, ErabliereWS, JeuxDonneesErabliereAPI).

Docker: `docker build -t erabliereapi:local .` at the repo root; `docker compose up -d` for a local deployment.

EF Core migrations (from `ErabliereApi/`, requires `dotnet-ef` and the `SQL_CONNEXION_STRING` + `USE_SQL` **machine environment variables** — the ef tool ignores launchSettings.json):
```
dotnet ef --startup-project . migrations add <Name> --output-dir "Depot\Sql\Migrations" --namespace "Depot.Sql.Migrations"
```

## Configuration is environment-variable driven

Nearly every feature is toggled by string-compared config values read in `Startup.cs` / `Extensions/ServiceCollectionExtension.cs` (e.g. `"true"`/`"false"` strings). Key ones:
- `USE_AUTHENTICATION` — turn auth on/off (locally: `dotnet user-secrets set USE_AUTHENTICATION false`).
- `USE_SQL`, `SQL_CONNEXION_STRING`, `SQL_USE_STARTUP_MIGRATION` — `false` runs fully in-memory (no persistence, handy for dev); `SQL_USE_STARTUP_MIGRATION=true` applies migrations at API startup.
- `USE_CORS`, `USE_HSTS`, `LOG_SQL`, `MiniProfiler.Enable`, `Stripe.*` (Stripe integration, webhooks at `/Checkout/Webhook` via the Stripe CLI).

Local secrets go through `dotnet user-secrets`, not appsettings.

## Backend architecture

- `Program.cs` → `Startup.cs`, which delegates most registration to extension methods in `Extensions/` (`AddErabliereApiControllers`, `AddErabliereAPIAuthentication`, `AddDatabase`, `AddHttpClients`, ...). New services are wired there, not inline in Startup.
- Controllers in `Controllers/` (one per resource, French names: `ErablieresController`, `CapteurController`, `DonneesCapteurController`, ...) expose **OData-style querying** (`$filter`, `$expand`, `$orderby`) consumed by the Angular app's `erabliereapi.service.ts`.
- Data access is a single EF Core `ErabliereDbContext` (`Depot/Sql/`), entity configurations in `Depot/Sql/EntityConfiguration/`, migrations in `Depot/Sql/Migrations/`.
- Cross-cutting concerns: `Authorization/` (API keys, customer access to érablières), `Middlewares/`, `Services/` (AI, ApiKey, Checkout/Stripe, IpInfo, LoRaWAN/ChirpStack, Nmap, Notifications, Users, Weather), Prometheus metrics, health checks, MiniProfiler.
- The API serves the built Angular app from `wwwroot/`; building the UI into it (`ng build --output-path="..\ErabliereApi\wwwroot\."`) gives a same-origin setup that preserves the custom `x-ddr`/`x-dde` delta-range headers used to minimize transferred sensor data.

### No entity binding on write endpoints (anti over-posting) — REQUIRED

Never bind an EF entity from `ErabliereApi.Donnees` (e.g. `Erabliere`, `Capteur`, `Arbre`, `Entaille`, `LigneTubelure`, ...) directly as the body parameter of a `POST`/`PUT`/`PATCH` action. Because EF Core traverses the object graph on `AddAsync`/`Update`, an authenticated attacker can populate an entity's **navigation properties** (`Erabliere`, `Entailles`, `Arbre`, ...) to create or modify rows in an érablière they don't own — bypassing `ValiderOwnership` and the sibling controllers' checks (this is what happened on the tubelure feature). The `ValiderOwnership("id")` filter and the `id != body.IdErabliere` guard only protect the root entity, never the nested children.

Instead:
- **Bind a dedicated DTO** from `ErabliereApi.Donnees.Action.Post` / `.Put` that contains only scalar fields and foreign-key ids — **no navigation properties** (see `PostEntaille`, `PutLigneTubelure`, `PostCapteur`, ...).
- **POST**: build the entity explicitly field-by-field before `AddAsync` (or `MapTo<TEntity>()`, which copies by name and thus can't set navigations the DTO doesn't have).
- **PUT/PATCH**: load the existing entity with `FindAsync`, verify it belongs to the route's érablière (`entity.IdErabliere != id → NotFound()`), then assign only the allowed fields. Do **not** call `_depot.Update(bodyEntity)` — it attaches the whole graph.
- Validate any FK the client supplies (e.g. `IdArbre`, `IdLigneTubelure`) belongs to the same érablière before saving.

This rule is enforced by `WriteEndpointsBindDtoNotEntityTest` in `ErabliereApi.Test`, which reflects over every controller and fails the build if a `POST`/`PUT`/`PATCH` action binds a type tracked as a `DbSet<>` on `ErabliereDbContext`. Several legacy controllers (`Baril`, `Dompeux`, `Alerte`, `DonneeCapteur`, `AlerteCapteur`, `ChirpStackSrvConfig`, `Donnee`, `IpInfo`) are grandfathered as known tech debt in that test's `ExceptionsConnues` list — do not add to it; migrate those to DTOs when you touch them (removing an entry is enforced too: a second test fails if an exception no longer matches a real violation).

## CI

GitHub Actions (`.github/workflows/`): `api-image.yml` (docker image), `api-test-demo.yml` (tests), `codeql-analysis.yml`, `proxy-publish.yaml` (NuGet proxy package).
