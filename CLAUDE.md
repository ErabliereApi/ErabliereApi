# CLAUDE.md

ErabliereApi is a maple-syrup-production (*érablière*) monitoring platform: an ASP.NET Core REST API
plus an Angular web app, collecting sensor data (temperature, vacuum, tank level), alerts, reports,
notes, and AI chat features.

Much of the codebase — identifiers, routes, comments, docs — is in **French**. Follow that convention.

This file routes; it holds no rules of its own. Follow the link for the area you're working in.

## Where to go

| Working on… | Read |
|---|---|
| **The web API** — controllers, services, auth, EF | [`ErabliereApi/CLAUDE.md`](ErabliereApi/CLAUDE.md) |
| **The Angular app** | [`ErabliereIU/CLAUDE.md`](ErabliereIU/CLAUDE.md) |
| **The MCP server** | [`ErabliereApi.Mcp/CLAUDE.md`](ErabliereApi.Mcp/CLAUDE.md) |
| **Entities and DTOs** | [`ErabliereModel/CLAUDE.md`](ErabliereModel/CLAUDE.md) |
| **The NuGet proxy client** | [`ErabliereAPI.Proxy/Readme.md`](ErabliereAPI.Proxy/Readme.md) — NSwag-generated, never hand-edited |
| **Deployment, k8s, docker** | [`Infrastructure/Readme.md`](Infrastructure/Readme.md) |
| **Device / data-feeding scripts** | [`PythonScripts/Readme.md`](PythonScripts/Readme.md) |

## Rules and reference

| Question | File |
|---|---|
| Adding a `POST`/`PUT`/`PATCH`? **Read this first.** | [`.claude/_shared/write-endpoint-dto.md`](.claude/_shared/write-endpoint-dto.md) |
| French naming, OData, `x-ddr`/`x-dde`, generated code | [`.claude/_shared/conventions.md`](.claude/_shared/conventions.md) |
| Env-var toggles, secrets, migrations, running the stack | [`.claude/_shared/configuration.md`](.claude/_shared/configuration.md) |
| Which test project, which command, CI | [`.claude/_shared/testing.md`](.claude/_shared/testing.md) |
| Building a feature end to end | [`.claude/workflows/feature-slice.md`](.claude/workflows/feature-slice.md) |
| ErabliereAI calling the MCP tools, and why it only sees the caller's data | [`Diagrams/ErabliereAI-Outils-MCP.md`](Diagrams/ErabliereAI-Outils-MCP.md) |

## Solution layout

| Project | Role |
|---|---|
| `ErabliereApi/` | The web API. Main project, .NET 9. Serves the built Angular app from `wwwroot/`. |
| `ErabliereModel/` | Data model (`ErabliereApi.Donnees`). One entity per file; `Erabliere` is the root of the hierarchy. |
| `ErabliereIU/` | Angular 22 front-end. |
| `ErabliereApi.Mcp/` | MCP server exposing the API to MCP clients. Read-only tools, stdio + HTTP transports. |
| `ErabliereAPI.Proxy/` | NSwag-generated C# client, published to NuGet. |
| `ErabliereApi.Test/` | Unit tests + the architecture guards. |
| `ErabliereApi.Integration.Test/` | `WebApplicationFactory` + AngleSharp; Stripe webhook fixtures. |
| `ErabliereApi.Mcp.Test/` | MCP tools, transports, plan gate. |
| `ErabliereApi.Test.Autofixture/` | Shared fixtures for the other test projects. |
| `Infrastructure/`, `Dockerfile`, `docker-compose*.yaml` | Kubernetes / docker deployment. |
| `PythonScripts/`, `Postman/`, `config/`, `Diagrams/` | Device scripts, API collections, auth templates, diagrams. |

## Quick commands

```powershell
dotnet build ErabliereApi.sln
dotnet test
.\start-light.ps1                    # API + Angular dev server
```

Everything else — migrations, coverage, docker, Stripe — is in
[`.claude/_shared/configuration.md`](.claude/_shared/configuration.md) and
[`.claude/_shared/testing.md`](.claude/_shared/testing.md).

Project tracking: https://dev.azure.com/freddycoder/ErabliereAPI
