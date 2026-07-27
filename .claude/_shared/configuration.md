# Configuration

Nearly every feature is toggled by a **string-compared** config value (`"true"` / `"false"`, not
booleans) read in `ErabliereApi/Startup.cs` and `ErabliereApi/Extensions/ServiceCollectionExtension.cs`.

## The toggles that change behaviour most

| Variable | Effect |
|---|---|
| `USE_AUTHENTICATION` | Turns auth on/off. Locally: `dotnet user-secrets set USE_AUTHENTICATION false`. |
| `USE_SQL` | `false` runs fully in-memory — no persistence, handy for dev. |
| `SQL_CONNEXION_STRING` | The connection string when `USE_SQL=true`. |
| `SQL_USE_STARTUP_MIGRATION` | `true` applies migrations at API startup. |
| `USE_CORS`, `USE_HSTS` | Transport-level toggles. Note CORS strips the `x-ddr`/`x-dde` headers. |
| `LOG_SQL`, `MiniProfiler.Enable` | Diagnostics. |
| `Stripe.*` | Stripe integration; webhooks arrive at `/Checkout/Webhook` via the Stripe CLI. |

## Secrets

Local secrets go through `dotnet user-secrets`, **never** appsettings.

## EF Core migrations

Run from `ErabliereApi/`. Requires `dotnet-ef`, plus `SQL_CONNEXION_STRING` and `USE_SQL` as
**machine environment variables** — the ef tool does not read `launchSettings.json`.

```powershell
dotnet ef --startup-project . migrations add <Name> `
  --output-dir "Depot\Sql\Migrations" --namespace "Depot.Sql.Migrations"
```

## Running the stack

| Command | What it starts |
|---|---|
| `dotnet watch run` in `ErabliereApi/` | API alone. Development: HTTP 5000 / HTTPS 5001. |
| `.\start-light.ps1` | API + Angular dev server (https://localhost:4200). `-startStripe $true` also forwards Stripe webhooks. |
| `.\start-local-debug-services.ps1` | The above plus Stripe CLI login/listen and optional sibling repos (EmailImagesObserver, ErabliereWS, JeuxDonneesErabliereAPI). |
| `docker compose up -d` | Local deployment. `docker build -t erabliereapi:local .` at the repo root. |

## MCP server configuration

`ErabliereApi.Mcp` is configured entirely through environment variables (`ERABLIEREAPI_URL`,
`ERABLIEREAPI_APIKEY`, `ERABLIEREAPI_MCP_TRANSPORT`) — with one exception, the plan gate, which lives
in `appsettings.json` because it's a mapping rather than a scalar. Full table in
[`ErabliereApi.Mcp/Readme.md`](../../ErabliereApi.Mcp/Readme.md#configuration).

## Auth templates

`config/oauth-oidc.template.json` and `config/oauth-oidc.template.aad.json`. The Angular app fetches
its own copy at **runtime** from `/assets/config/oauth-oidc.json` — changing the target API or auth
mode there needs no rebuild. See `ErabliereIU/CLAUDE.md`.
