# ErabliereApi.Mcp — MCP server over ErabliereAPI

A Model Context Protocol server exposing ErabliereAPI to an MCP client. Talks to a **running** API
through the `ErabliereAPI.Proxy` NuGet client. All tools are read-only.

**[`Readme.md`](Readme.md) is the contract for this project** — tool table, response envelope,
configuration variables, plan gating, docker, client setup, and the implementation notes explaining
*why* each piece is the way it is. Read the relevant section there rather than inferring from code;
this file only routes.

| You're doing… | Read |
|---|---|
| Adding or changing a tool | [Tools](Readme.md#tools) · [Response envelope](Readme.md#response-envelope) |
| Anything touching sensor data | [Sensor data is summarized, never dumped](Readme.md#sensor-data-is-summarized-never-dumped) |
| Transport, hosting, api keys | [HTTP transport](Readme.md#http-transport) · [Why it runs stateless](Readme.md#why-it-runs-stateless) |
| Subscription gating | [Plan gating](Readme.md#plan-gating) |
| Container or deployment | [Docker](Readme.md#docker) |
| Debugging by hand | [Manual verification with the MCP inspector](Readme.md#manual-verification-with-the-mcp-inspector) |
| Any non-obvious design choice | [Implementation notes](Readme.md#implementation-notes) |

## Two projects

The tool set is a **library**, `ErabliereApi.Mcp.Tools`, because it has two consumers: this server,
and the ErabliereAI chat inside `ErabliereApi`. The executable keeps only what is about *being a
server*. Referencing the executable instead would drag its `appsettings.json` and
`docker-entrypoint.sh` into the API's publish output, where they collide with its own (NETSDK1152).

| Project / folder | Contents |
|---|---|
| **`ErabliereApi.Mcp.Tools`** `Tools/` | The curated tool set — `ErabliereTools`, `CapteurTools`, `AlerteTools`, `NoteTools`, `RapportTools`, `BarilTools`, `DompeuxTools`, `HoraireTools`, `PlanTools`, `ToolArguments`. |
| `Models/`, `Serialization/` | Projections, `ToolResponse`, `ToolJson`. |
| `Services/`, `Configuration/` | `DonneesCapteurSummarizer`, the plan resolution, `ErabliereApiMcpOptions`, `McpPlanGatingOptions`. |
| `Http/`, `Hosting/` | `IApiKeyAccessor`, `ApiKeyHandler`, `McpTransportMode` — the parts the options and the tools need. |
| **`ErabliereApi.Mcp`** `Hosting/` | `StdioServerRunner`, `HttpServerRunner`, `McpTransportSelector`. `Program.cs` only picks a transport. |
| `Http/` | The two `IApiKeyAccessor` implementations, `RequireApiKeyMiddleware`, `RequirePlanMiddleware`, `JsonRpcErrorWriter`. |
| `Health/`, `Extensions/`, `appsettings.json` | Health endpoints, registration, plan gating configuration. |

## Three rules that break things silently

- **stdout is the JSON-RPC channel.** All logging is forced to stderr in `Hosting/StdioServerRunner`.
  Writing anything to stdout breaks the client. (HTTP has no such constraint.)
- **The tool set is curated, not generated.** It is deliberately *not* one tool per controller — a
  model picks worse the more tool definitions it reads. A new tool must earn its context cost.
- **Every response stays under ~4000 tokens.** `Models/ToolResponse` measures the payload with the
  same `JsonSerializerOptions` handed to `WithToolsFromAssembly`, so the budget is a guarantee.
  List payloads are trimmed from the tail until they fit.

## Commands

```powershell
dotnet build ErabliereApi.Mcp\ErabliereApi.Mcp.csproj      # tire ErabliereApi.Mcp.Tools
dotnet test ErabliereApi.Mcp.Test\ErabliereApi.Mcp.Test.csproj
docker build -t erabliereapi/erabliereapi-mcp:local -f ErabliereApi.Mcp\Dockerfile .   # from the REPO ROOT
```

The docker build runs the unit **and** integration tests, so a failing MCP test fails the image build.
The build context needs `ErabliereAPI.Proxy`, hence the repo root.

## The tool set has two consumers

`ErabliereApi` references `ErabliereApi.Mcp.Tools` and hands the same `[McpServerTool]` methods to
the ErabliereAI chat (`ErabliereApi/Services/AI/Tools/`). Changing a tool name, description or schema
changes what the chat can do, not only what an MCP client sees. `ErabliereApi.Test/ErabliereAiToolCatalogTest.cs`
pins the exposed set and fails on anything not marked `ReadOnly`.

→ [Diagrams/ErabliereAI-Outils-MCP.md](../Diagrams/ErabliereAI-Outils-MCP.md)

## Depends on the API, in both directions

Tools call the API through the proxy. The plan gate calls `GET /api/Abonnements/Courant`, which was
added to ErabliereAPI *for it* and answers from the same `IAbonnementService` that
`ValiderAbonnementAttribute` uses — so plan logic lives in exactly one place, over there. Don't
reimplement it here.
