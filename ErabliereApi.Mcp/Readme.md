# ErabliereApi.Mcp

A [Model Context Protocol](https://modelcontextprotocol.io) server exposing ErabliereAPI to an
MCP client (Claude Code, Claude Desktop, ...). It talks to a running ErabliereAPI instance through
the `ErabliereAPI.Proxy` NuGet client.

Two transports, one tool set:

| Transport | Started with | Who runs it | Where the api key comes from |
| --- | --- | --- | --- |
| **stdio** (default) | no argument, or `--stdio` | the MCP client, as a child process | `ERABLIEREAPI_APIKEY`, once, at startup |
| **Streamable HTTP** | `--http`, or `ERABLIEREAPI_MCP_TRANSPORT=http` | you, as a hosted service | the `X-ErabliereApi-ApiKey` header of each request |

The tool classes are shared verbatim; only the transport, the api key plumbing and the
[plan gate](#plan-gating) differ — the hosted transport can be restricted to the subscription plans
that include MCP access, the self-hosted one never is.

All the tools are read-only.

### A third consumer: the ErabliereAI chat

`ErabliereApi` references `ErabliereApi.Mcp.Tools` — the library holding the tool set, not this
executable — and turns the same `[McpServerTool]` methods into tool definitions for the chat of the
web application, so a user asking "quelle était la température de mon érablière hier ?" gets an answer
read from their real data. No MCP server and no transport is started inside the API: only the tool
classes are reused, and their `IErabliereAPIProxy` is one pointing back at the API with the
credentials of the user being served.

That means **a change to a tool here changes what the chat can do**, and the tool descriptions are
read by two audiences. Two guards live on the API side: `ErabliereAiToolCatalogTest` pins the exposed
set (12 of the 13 — `get_my_plan` is not offered, it answers about the MCP client rather than about
the maple grove) and refuses anything not marked `ReadOnly`.

Details and the authorization argument: [Diagrams/ErabliereAI-Outils-MCP.md](../Diagrams/ErabliereAI-Outils-MCP.md).

## Tools

The set is deliberately **curated**: it is not one tool per controller. A model spends context
reading tool definitions and picks worse the more of them there are, so each entry has to earn its
place.

| Tool | Description |
| --- | --- |
| `list_erablieres` | Lists the maple groves the configured API key can read. Optional `nameContains` filter and `top` (1-100, default 25). |
| `get_erabliere` | Gets a single maple grove by identifier (`erabliereId`, a GUID). |
| `get_alertes` | Lists the alerts of a maple grove: thresholds, recipients, enabled state, last occurrence. |
| `get_alertes_capteur` | Lists the alerts configured on the **sensors** of a maple grove: min/max bound, watched sensor, recipients, enabled state, last occurrence. |
| `list_capteurs` | Lists the sensors with their **unit**, kind, connectivity and battery level. Required to get a sensor identifier. |
| `get_donnees_capteur` | Summarizes the readings of one sensor over a **mandatory** date range. Never returns a raw dump. |
| `get_dompeux` | Lists the dumping events (tank emptying cycles) with their duration. |
| `get_notes` | Lists the producer's journal, most recent first, with a keyword search. |
| `list_rapports` | Lists the saved reports with their period and aggregates, rows excluded. |
| `get_rapport` | Gets one report with its aggregates and its daily rows. |
| `get_barils` | Lists the barrels closed over a range with their syrup grades. |
| `get_horaire` | Gets the weekly opening hours. |
| `get_my_plan` | Gets the subscription plan of the account owning the API key and what it grants on this server. |

All of them are annotated `readOnlyHint` / `idempotentHint`, so a client may run them without asking
for confirmation.

> `get_erabliere` is implemented with the OData filter of the list endpoint, because ErabliereAPI
> has no `GET /Erablieres/{id}` route.

> Alerts come in two kinds and so do the tools. An `Alerte` belongs to the maple grove and carries one
> threshold per kind of measurement (temperature, vacuum, tank level) — that is `get_alertes`. An
> `AlerteCapteur` watches **one** sensor between a `minValue` and a `maxValue` expressed in the unit
> of that sensor — that is `get_alertes_capteur`. Merging them would have produced a payload where
> half the fields are always null.

### Response envelope

Every tool returns the same shape, serialized compactly:

```json
{ "summary": "…", "data": …, "truncated": false }
```

- **`summary`** is one sentence digesting the payload. A model can very often answer the user from
  this line alone, without walking `data`.
- **`data`** is the payload: a list for the listing tools, an object for the others.
- **`truncated`** is true when the answer is incomplete — either the API capped the query, or the
  payload had to be trimmed to fit the budget. The `summary` then says what to do about it.

Every response is kept under **~4000 tokens**. `Models/ToolResponse` measures the payload with the
very `JsonSerializerOptions` handed to `WithToolsFromAssembly`, so the budget is a guarantee and not
an estimate; list payloads are trimmed from the tail until they fit.

### Sensor data is summarized, never dumped

A sensor reporting every five minutes produces about 8 600 readings a month. `get_donnees_capteur`
therefore returns statistics plus a series downsampled to at most 100 averaged points (`maxPoints`,
1-200):

```json
{
  "summary": "Sensor 'Température extérieure': 576 readings from 2026-03-12T00:00:00-04:00 to 2026-03-13T23:55:00-04:00, min -14 °C, max 6 °C, average -4 °C, latest -4.2 °C. The serie is downsampled to 8 averaged points.",
  "data": {
    "count": 576, "unit": "°C", "min": -14, "max": 6, "avg": -4, "latest": -4.2,
    "first": "2026-03-12T00:00:00-04:00", "last": "2026-03-13T23:55:00-04:00", "latestText": null,
    "serie": [{ "t": "2026-03-12T00:00:00-04:00", "v": 2.3 }, "…"],
    "serieIsDownsampled": true
  },
  "truncated": false
}
```

Two days of readings come back as 785 characters, roughly 196 tokens. Points are bucket **averages**,
so a spike can be smoothed out — the true extremes always survive in `min` and `max`.

The date range is mandatory, and both bounds are ISO 8601 (`2026-03-12` or
`2026-03-12T06:30:00-04:00`). A date without a time is read as **local** midnight, because a model
writing `2026-03-12` means that day at the maple grove.

## Configuration

Everything is configured through environment variables: in stdio mode the MCP client starts this
server as a child process and the environment is the only channel it has, and the container image
follows the same convention.

| Variable | stdio | HTTP | Description |
| --- | --- | --- | --- |
| `ERABLIEREAPI_URL` | **required** | **required** | Absolute base url of the API, e.g. `https://erabliereapi.freddycoder.com` or `https://localhost:5001` for a local run. |
| `ERABLIEREAPI_APIKEY` | **required** | ignored | API key sent in the `X-ErabliereApi-ApiKey` header. Create one in the web application under *Profil -> Clés d'api*. Over HTTP each client sends its own, so the server holds none. |
| `ERABLIEREAPI_MCP_TRANSPORT` | – | `http` | Selects the transport. `--http` / `--stdio` on the command line wins over it. |
| `ASPNETCORE_HTTP_PORTS` / `ASPNETCORE_URLS` | – | optional | Standard Kestrel binding. The container image sets `8080`. |

The server exits with code `1` and prints what is missing on stderr when a variable is absent or
malformed. Restrict the key to the `GET` verb when creating it: the server never writes.

The **plan gate** of the HTTP transport is the one thing configured through `appsettings.json`
rather than the environment, because it is a mapping and not a scalar. See
[Plan gating](#plan-gating) below. Every key of it stays overridable the usual way
(`Mcp__PlanGating__Enabled=true`).

## HTTP transport

```powershell
$env:ERABLIEREAPI_URL = "https://erabliereapi.freddycoder.com"
dotnet run --project ErabliereApi.Mcp\ErabliereApi.Mcp.csproj -- --http
```

| Endpoint | Api key | Description |
| --- | --- | --- |
| `POST /mcp` | **required** | The MCP [Streamable HTTP](https://modelcontextprotocol.io/specification/2025-06-18/basic/transports) endpoint. Restricted by subscription plan when the [plan gate](#plan-gating) is on. |
| `GET /live` | no | Liveness: the process is serving. |
| `GET /health` | no | Readiness: the process is serving **and** ErabliereAPI answers on its own `/health`. |

### Authentication

There is no new authentication scheme. The MCP client sends the same `X-ErabliereApi-ApiKey` header
ErabliereAPI itself reads, and the server relays it on every call it makes on that client's behalf.

The header is required by the whole `/mcp` endpoint, not only by the tool calls: `initialize` and
`tools/list` are gated too, because the tool catalog describes the maple grove features of an
account. A request without the header gets a `401` and a `WWW-Authenticate: ApiKey` response.

The server never validates the key by itself. ErabliereAPI owns the api key table and answers
`401`/`403` on the first call, which keeps one authority for revocation and usage tracking. With the
plan gate on, that first call happens before the request is served, so a revoked key is caught on
`initialize`.

### Plan gating

A hosted server answers whoever holds an api key, and the tools it exposes cost real queries on the
API. The gate restricts it to the subscription plans that include MCP access.

It is **off by default**: an existing deployment upgrades without a change in behaviour, and does not
pay one extra call to ErabliereAPI per request. Same progressive rollout as the API's own
`Abonnement.ValiderPlan`.

```json
{
  "Mcp": {
    "PlanGating": {
      "Enabled": true,
      "RequiredCapability": "mcp",
      "DefaultPlan": "gratuit",
      "CacheDuration": "00:05:00",
      "SubscriptionUrl": "https://erabliereapi.freddycoder.com/abonnement",
      "PlanCapabilities": {
        "gratuit": [],
        "base": [ "mcp" ]
      }
    }
  }
}
```

| Key | Default | Description |
| --- | --- | --- |
| `Enabled` | `false` | Turns the gate on. Everything below is only read when it is on. |
| `RequiredCapability` | `mcp` | The capability a plan must hold to connect. |
| `DefaultPlan` | `gratuit` | Plan of an account carrying no active subscription. |
| `PlanCapabilities` | – | What each plan may do, keyed by the plan name of `ForfaitsAbonnement` (`gratuit`, `base`). A plan absent from the map is granted nothing. |
| `CacheDuration` | `00:05:00` | How long a resolved plan is reused before ErabliereAPI is asked again. `00:00:00` disables the cache. |
| `SubscriptionUrl` | – | Optional link added to the denial message. |

In a container, the same values as environment variables:

```
Mcp__PlanGating__Enabled=true
Mcp__PlanGating__PlanCapabilities__base__0=mcp
```

**Where the plan comes from.** No plan logic lives in this project. The server calls
`GET /api/Abonnements/Courant` with the caller's own api key, and that endpoint answers from the very
`IAbonnementService` the API's `ValiderAbonnementAttribute` uses, so the two can never disagree on
who is on what plan.

**What a denied caller gets.** A JSON-RPC error, not a bare `403`:

```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "error": {
    "code": -32003,
    "message": "This account has no active subscription, so it is on the 'gratuit' plan, which does not include access to the ErabliereAPI MCP server. Reaching it requires the 'base' plan. Subscribe at https://erabliereapi.freddycoder.com/abonnement.",
    "data": { "currentPlan": "gratuit", "requiredCapability": "mcp", "plansGrantingAccess": "base", "subscriptionUrl": "…" }
  }
}
```

An MCP client turns a non-2xx answer into a transport failure and usually drops the body, so a `403`
would reach the user as "the server returned 403" and never as the sentence telling them what to
subscribe to. The status stays `200` and the reason is also copied on the
`X-ErabliereApi-Mcp-Denied-Reason` response header, for logs and monitoring. Code `-32004` is the
neighbouring case: the plan could not be read at all (key refused, account not identifiable, API
unreachable).

**It gates the whole endpoint**, `initialize` and `tools/list` included: a plan that opens nothing
should not read the tool catalog either, since it names the features of an account.

**It fails closed.** When ErabliereAPI cannot be reached the plan is unknown, and letting an unknown
plan through would make the gate a suggestion: a client pointed at an unreachable API would gain the
access the configuration denies it.

**Stdio is never gated.** A stdio server is started by the user on their own machine, with their own
key, and answers no one else; charging a plan to run a process they host would gate nothing. The
gate is a piece of the HTTP pipeline, and a stdio server has no HTTP pipeline.

**Requires the Stripe integration to be on.** ErabliereAPI only ties an api key back to a customer
when Stripe is enabled (`UsersUtils.GetUniqueName`). Without it, `GET /api/Abonnements/Courant`
answers `404` and every caller is refused with the `-32004` message, which says as much. A server
running without Stripe has no subscriptions to gate on and should leave `Enabled` at `false`.

The server refuses to start when the gate is on and no plan holds the required capability: that
configuration locks out every caller, operator included.

### Why it runs stateless

`HttpServerTransportOptions.Stateless` is on, so no session state is kept between requests: any
instance can answer any request and no load balancer needs session affinity. It also means a session
opened with one api key can never be replayed with another, since every request carries its own.
The trade-off is that server-initiated messages (sampling, elicitation, roots) are unavailable —
this server is a read-only tool provider and uses none of them.

## Docker

```powershell
docker build -t erabliereapi/erabliereapi-mcp:local -f ErabliereApi.Mcp\Dockerfile .
docker run --rm -p 5011:8080 -e ERABLIEREAPI_URL=https://erabliereapi.freddycoder.com erabliereapi/erabliereapi-mcp:local
```

Build from the **root of the repository**: the context needs `ErabliereAPI.Proxy`. The image runs
the unit and integration tests during the build, serves HTTP on `8080` and declares a `HEALTHCHECK`
on `/live`.

The container runs as the unprivileged `app` user (uid 1654) of the dotnet base images, not as
root: the server listens on the network, so a compromise of it should not be able to rewrite the
image or install packages. Only `/usr/local/share/ca-certificates` and `/etc/ssl/certs` are owned
by `app`, because the entrypoint writes the mounted development CA there.

`docker-compose.yaml` at the root starts it next to the API as `erabliere-mcp`, published on
`http://localhost:5011`:

```powershell
docker compose up -d erabliere-api erabliere-mcp
```

Inside the compose network it reaches the API at `https://erabliere-api`; the entrypoint trusts the
development root CA mounted at `/https-root/aspnetapp-root-cert.cer`, the same way the API image
does. Nothing is mounted in production, where the API has a real certificate, and the step is
skipped.

## Build and run

```powershell
dotnet build ErabliereApi.Mcp\ErabliereApi.Mcp.csproj
dotnet test ErabliereApi.Mcp.Test\ErabliereApi.Mcp.Test.csproj
```

For a self-contained deployment used by an MCP client, publish it once and point the client at the
resulting executable:

```powershell
dotnet publish ErabliereApi.Mcp\ErabliereApi.Mcp.csproj -c Release -o C:\Tools\ErabliereApi.Mcp
```

## MCP client configuration

### Remote server, over HTTP

Nothing is installed on the client side: point it at the hosted endpoint and give it the api key.

```powershell
claude mcp add --transport http erabliereapi https://mcp.erabliereapi.freddycoder.com/mcp --header "X-ErabliereApi-ApiKey: <your-api-key>"
```

Or in `.mcp.json`, which every MCP client that speaks Streamable HTTP understands:

```json
{
  "mcpServers": {
    "erabliereapi": {
      "type": "http",
      "url": "https://mcp.erabliereapi.freddycoder.com/mcp",
      "headers": {
        "X-ErabliereApi-ApiKey": "<your-api-key>"
      }
    }
  }
}
```

Against the local docker compose deployment the url is `http://localhost:5011/mcp`.

### Local server, over stdio

#### Claude Code

Add the server to the project with the CLI:

```powershell
claude mcp add erabliereapi --env ERABLIEREAPI_URL=https://erabliereapi.freddycoder.com --env ERABLIEREAPI_APIKEY=<your-api-key> -- dotnet run --project C:\path\to\ErabliereApi\ErabliereApi.Mcp\ErabliereApi.Mcp.csproj
```

Or declare it in a `.mcp.json` file:

```json
{
  "mcpServers": {
    "erabliereapi": {
      "command": "dotnet",
      "args": [
        "run",
        "--project",
        "C:\\path\\to\\ErabliereApi\\ErabliereApi.Mcp\\ErabliereApi.Mcp.csproj"
      ],
      "env": {
        "ERABLIEREAPI_URL": "https://erabliereapi.freddycoder.com",
        "ERABLIEREAPI_APIKEY": "<your-api-key>"
      }
    }
  }
}
```

#### Claude Desktop

In `claude_desktop_config.json`, using the published binary (recommended, `dotnet run` rebuilds the
project on every start):

```json
{
  "mcpServers": {
    "erabliereapi": {
      "command": "C:\\Tools\\ErabliereApi.Mcp\\ErabliereApi.Mcp.exe",
      "args": [],
      "env": {
        "ERABLIEREAPI_URL": "https://erabliereapi.freddycoder.com",
        "ERABLIEREAPI_APIKEY": "<your-api-key>"
      }
    }
  }
}
```

## Manual verification with the MCP inspector

Over stdio:

```powershell
$env:ERABLIEREAPI_URL = "https://localhost:5001"
$env:ERABLIEREAPI_APIKEY = "<your-api-key>"
npx @modelcontextprotocol/inspector dotnet run --project ErabliereApi.Mcp\ErabliereApi.Mcp.csproj
```

Over HTTP, start the server first, then connect the inspector to `http://localhost:5099/mcp` in
*Streamable HTTP* mode and add the `X-ErabliereApi-ApiKey` header under *Authentication*:

```powershell
$env:ERABLIEREAPI_URL = "https://localhost:5001"
$env:ASPNETCORE_URLS = "http://localhost:5099"
dotnet run --project ErabliereApi.Mcp\ErabliereApi.Mcp.csproj -- --http
npx @modelcontextprotocol/inspector
```

The inspector opens a web page: connect, open the *Tools* tab, then

1. run `list_erablieres` and copy an `id` from `data`;
2. call `get_erabliere`, `get_alertes`, `get_alertes_capteur`, `get_horaire`, `get_notes`,
   `list_rapports` and `get_barils` with it — on `get_alertes_capteur`, check every entry carries a
   `capteurNom` and a `capteurSymbole`, and that lowering `top` below the number of alerts turns
   `truncated` true;
3. run `list_capteurs` with the same id and copy a sensor `id`;
4. call `get_donnees_capteur` with both ids and a range covering a day the sensor was reporting,
   for example `startDate=2026-03-12` and `endDate=2026-03-13`; check `data.count`, `data.unit` and
   that `data.serie` holds at most 100 points;
5. call it again over a month to see `truncated` turn true and the `summary` ask for a narrower
   range;
6. call it with `endDate` before `startDate`, and with a missing `startDate`, to check the errors
   come back as readable sentences rather than stack traces;
7. run `get_my_plan` and check `data.plan` matches the subscription of the account owning the key.

A local ErabliereAPI can be started with `.\start-light.ps1` at the root of the repository.

### Checking the plan gate by hand

Start the API with Stripe enabled and the subscriptions on, then the MCP server with the gate on:

```powershell
$env:ERABLIEREAPI_URL = "https://localhost:5001"
$env:ASPNETCORE_URLS = "http://localhost:5099"
$env:Mcp__PlanGating__Enabled = "true"
dotnet run --project ErabliereApi.Mcp\ErabliereApi.Mcp.csproj -- --http
```

With an api key of an account **without** an active `base` subscription:

```powershell
curl -s -X POST http://localhost:5099/mcp `
  -H "X-ErabliereApi-ApiKey: <your-api-key>" `
  -H "Content-Type: application/json" `
  -H "Accept: application/json, text/event-stream" `
  -d '{\"jsonrpc\":\"2.0\",\"id\":1,\"method\":\"tools/list\"}'
```

The answer is `200` carrying a JSON-RPC `error` of code `-32003` naming the `base` plan. Subscribe
the account under *Profil -> Abonnement* in the web application, wait out `CacheDuration`, and the
same call answers the tool list. The `Accept` header is not optional: the Streamable HTTP transport
answers `406` without both media types.

## Implementation notes

- **stdout is the JSON-RPC channel.** All logging is forced to stderr in `Hosting/StdioServerRunner`;
  writing anything on stdout breaks the client. The HTTP transport has no such constraint and logs
  normally.
- **One tool set, two runners.** `Program.cs` only picks a transport; `Hosting/StdioServerRunner` and
  `Hosting/HttpServerRunner` compose the rest. The tools, the projections and the serializer know
  nothing about either. The transport switches are stripped from the arguments before they reach the
  host builder: the command line configuration provider expects `--key=value` pairs and throws on a
  lone `--http`.
- **The project is still a console application.** `Microsoft.NET.Sdk` plus an explicit
  `FrameworkReference` to `Microsoft.AspNetCore.App`, rather than `Microsoft.NET.Sdk.Web`: stdio is
  the default and the HTTP bits are opt-in.
- **Authentication.** `ErabliereAPI.Proxy` only ships an Azure AD client credentials handler, so
  `Http/ApiKeyHandler.cs` adds the `X-ErabliereApi-ApiKey` header expected by the API's
  `ApiKeyMiddleware`. The retry handler of the proxy project is reused as-is, with a 2 second delay
  instead of the 30 second default, because an MCP client waits synchronously for the tool result.
- **Where the key comes from is the only thing the transports disagree on.** `IApiKeyAccessor` has
  two implementations: `ConfiguredApiKeyAccessor` reads the environment, `HttpHeaderApiKeyAccessor`
  reads the current request. The second one is a singleton over `IHttpContextAccessor` on purpose:
  `IHttpClientFactory` builds its message handlers in a handler scope of its own rather than in the
  request scope, and the `AsyncLocal` behind `IHttpContextAccessor` is the one way to cross that
  boundary. It works because `PerSessionExecutionContext` is left at its default of `false`, which
  the SDK documents as running a tool handler with the `HttpContext` of the request that carried the
  `tools/call`.
- **The proxy is scoped over HTTP, singleton over stdio.** The MCP SDK opens a service scope per
  request (`McpServerOptions.ScopeRequests`), so no proxy instance is ever shared between two api
  keys. `HttpTransportTest` pins this down: two clients calling the same tool must produce two calls
  to ErabliereAPI carrying their own key, in order.
- **Results are projections.** The proxy DTOs carry a dozen navigation collections that are always
  null on these endpoints. The records in `Models/` only expose the meaningful scalar fields, which
  keeps the tool results small in the model context.
- **Attachments never reach the model.** The notes endpoint returns the attached file inline, base 64
  encoded; one photograph is worth more tokens than a whole response is allowed to be. `get_notes`
  passes an explicit `$select` so the bytes never leave the API, and `NoteSummary` exposes only the
  attachment metadata.
- **One serializer.** `Serialization/ToolJson` is handed to `WithToolsFromAssembly` and used to
  measure the response budget. It writes dates as ISO 8601 truncated to the second, dropping the
  seven fractional digits the database returns on every reading, and leaves the French accents
  unescaped instead of paying six characters each for them.
- **Sensor readings are capped at the source.** `GET /Capteurs/{id}/DonneesCapteurV2` applies no
  limit of its own when `top` is omitted, so `get_donnees_capteur` always sends one. Hitting that cap
  flags the response as `truncated` rather than quietly summarizing a partial window.
- **The plan gate consumes the subscriptions, it does not reimplement them.** `GET
  /api/Abonnements/Courant` was added to ErabliereAPI for it and answers from `IAbonnementService`,
  the same service `ValiderAbonnementAttribute` now calls, so the rule "what plan is this user on"
  lives at exactly one place. The endpoint also had to recognise an api key caller, which meant
  reading `ApiKeyAuthorizationContext` from the request scope: it is registered `Scoped` and filled
  by `ApiKeyMiddleware` in that scope, so a child scope created with `CreateScope()` receives an
  empty instance and identifies no one.
- **The plan is not read through `ErabliereAPI.Proxy`.** The proxy is generated with NSwag Studio
  from the OpenAPI document and predates the subscription feature, so the call is a plain
  `HttpClient` one on the named client the tools use — which means it carries the caller's key and
  is retried like the rest. It should move to the proxy the next time it is regenerated.
- **A resolved plan is cached for five minutes, per key.** An MCP session sends many requests and a
  plan changes a few times a year; without the cache every `tools/call` would cost an extra round
  trip and a database query. The cache key is a SHA-256 of the api key, never the key itself: a
  cache entry can end up in a dump, and a credential should not. The expiry is absolute and not
  sliding, otherwise a busy session would never re-check and a cancelled subscription would keep its
  access forever.
- **`get_alertes_capteur` includes the sensor, and paginates itself.** `GET
  /Erablieres/{id}/AlertesCapteur` takes no OData argument at all: no `$top`, no `$orderby`. The
  bound of a sensor alert is a bare number, meaningless without the name and the unit of what it
  watches, so the tool asks for `include=Capteur` — the same call the web application makes on that
  route — and flattens the sensor into `capteurNom` / `capteurSymbole`. The order and the `top` are
  then applied client-side: sorting by sensor groups the alerts the way a producer reads them, and
  makes the tail that gets cut off the same one on two identical calls. Cutting the list sets
  `truncated` and the summary says how many alerts exist in total.
- **`get_dompeux` reads ascending.** The API implements its descending order as a `Reverse()` over an
  unordered EF query, which is not something to depend on, so the tool always asks for `o=c` and says
  in its description that a recent range needs a later `startDate`.
