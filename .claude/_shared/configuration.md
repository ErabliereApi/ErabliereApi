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

## ErabliereAI tools

`ErabliereAI:Tools:*` in `appsettings.json` bounds the tool calling loop of the chat — `Enabled`,
`MaxRounds`, `ToolTimeout`, `TokenBudget`, `Temperature`, `ExcludedTools`, `ApiBaseUrl`,
`ActivityRetention`. `Temperature` overrides `LLMDefaultTemperature` for the completions of a tool
driven exchange only: the platform default of 1 suits a conversation and makes a tool loop invent
search terms and date ranges. Which
plans may use the tools is **not** configured there: the chat reads the same `Mcp:PlanGating` section
as the MCP server, so one deployment decision covers both. Table in
[`README.md`](../../README.md#configuration), rationale in
[`Diagrams/ErabliereAI-Outils-MCP.md`](../../Diagrams/ErabliereAI-Outils-MCP.md).

`ApiBaseUrl` is the one to reach for when the tools stop working behind a proxy: left empty, they
call back the address of the request being served, which a TLS-terminating ingress can make
unreachable from inside the cluster.

`PrimaryAIService` picks the provider — `Google` for Gemini (`GoogleGenAIKey`, `GoogleGenAIModel`),
anything else for Azure OpenAI. Both run the tool loop; `GoogleGenAIEnableToolCalling=false` takes
the tools away from Gemini alone, leaving the chat answering from the model's own knowledge.

## Auth templates

`config/oauth-oidc.template.json` and `config/oauth-oidc.template.aad.json`. The Angular app fetches
its own copy at **runtime** from `/assets/config/oauth-oidc.json` — changing the target API or auth
mode there needs no rebuild. See `ErabliereIU/CLAUDE.md`.
