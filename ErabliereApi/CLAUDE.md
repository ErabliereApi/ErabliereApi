# ErabliereApi — the ASP.NET Core web API

The main project. Targets .NET 9 (`global.json` pins SDK 10 with `rollForward: latestMajor`). Also
serves the built Angular app from `wwwroot/`.

## Before you write a POST, PUT, or PATCH

Read **[.claude/_shared/write-endpoint-dto.md](../.claude/_shared/write-endpoint-dto.md)**. Binding
an EF entity on a write endpoint fails the build and, worse, opens a cross-tenant over-posting hole.
Bind a DTO from `ErabliereModel/Action/`.

## Layout

| Folder | Contents |
|---|---|
| `Controllers/` | 32 controllers, one per resource, French names. `Base/ErabliereApiBaseController.cs` is the shared base. |
| `Depot/Sql/` | The single EF Core `ErabliereDbContext`, `EntityConfiguration/`, `Migrations/`. See its `Readme.md`. |
| `Extensions/` | Service registration. `ServiceCollectionExtension.cs` is where new services get wired. |
| `Services/` | `AI` (dont `AI/Tools/`, les outils MCP appelés par la conversation), `Abonnements`, `ApiKey`, `Checkout` (Stripe), `IpInfo`, `LoRaWAN` (ChirpStack), `Nmap`, `Notifications`, `Users`, `Weather`. |
| `Authorization/` | `ApiKeyAuthorization/` (middleware, handler, scoped `ApiKeyAuthorizationContext`), `Customers/`, `Policies/`. |
| `Attributes/` | `ValiderOwnershipAttribute`, `ValiderAbonnementAttribute`, `ValiderIPRulesAttributes`, `SecureEnableQueryAttribute`, the `TriggerAlert` family (V3/V4), `Validators/`. |
| `Middlewares/` | `GlobalExceptionHandler`, `IpInfoMiddleware`, `ODataCountHeaderMiddleware`, `ChaosEngineeringMiddleware`. |
| `HealthCheck/`, `OperationFilter/`, `Formaters/`, `ControllerFeatureProviders/`, `Resources/` | Cross-cutting plumbing. |

## Startup path

`Program.cs` → `Startup.cs` → extension methods in `Extensions/` (`AddErabliereApiControllers`,
`AddErabliereAPIAuthentication`, `AddDatabase`, `AddHttpClients`, …). **Register new services in the
extension methods, not inline in `Startup.cs`.**

Behaviour is driven by string-compared environment variables — `USE_AUTHENTICATION`, `USE_SQL`,
`USE_CORS`, `Stripe.*`. Full table: [.claude/_shared/configuration.md](../.claude/_shared/configuration.md).

## Querying

Controllers expose OData (`$filter`, `$expand`, `$orderby`) consumed by the Angular app. Use
`SecureEnableQueryAttribute` rather than a bare `[EnableQuery]`.

The custom `x-ddr` / `x-dde` delta-range headers minimize transferred sensor data. CORS strips them,
so verify them same-origin by building the UI into `wwwroot/`.

## Ownership is the security model

`Erabliere` is the root of the hierarchy. `ValiderOwnership("id")` checks the route's érablière
against the caller — it does **not** check nested entities in a request body. Validate every
client-supplied FK yourself.

`ApiKeyAuthorizationContext` is registered **Scoped** and filled by `ApiKeyMiddleware` in the request
scope. A child scope from `CreateScope()` gets an empty instance and identifies nobody.

**ErabliereAI is not an exception to any of this.** Its tools call the API back over HTTP with the
caller's own `Authorization` / `X-ErabliereApi-ApiKey` header (`Services/AI/Tools/CallerCredentialsHandler.cs`).
The AI holds no credential of its own, so it reaches exactly what its user reaches — never give it
one. → [Diagrams/ErabliereAI-Outils-MCP.md](../Diagrams/ErabliereAI-Outils-MCP.md)

## Commands

```powershell
dotnet build ErabliereApi.sln
dotnet watch run          # from this folder — HTTP 5000 / HTTPS 5001 in Development
dotnet test ErabliereApi.Test
```

Migrations, run from this folder, need `SQL_CONNEXION_STRING` + `USE_SQL` as **machine** environment
variables — see [configuration.md](../.claude/_shared/configuration.md).

## See also

- [.claude/workflows/feature-slice.md](../.claude/workflows/feature-slice.md) — the full stack walk
- [.claude/_shared/testing.md](../.claude/_shared/testing.md) — which test project, which command
