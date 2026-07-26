# ErabliereApi.Mcp

A [Model Context Protocol](https://modelcontextprotocol.io) server exposing ErabliereAPI to an
MCP client (Claude Code, Claude Desktop, ...). It talks to a running ErabliereAPI instance through
the `ErabliereAPI.Proxy` NuGet client, and communicates with the MCP client over **stdio**.

Phases 1 and 2 expose read-only tools only.

## Tools

The set is deliberately **curated**: it is not one tool per controller. A model spends context
reading tool definitions and picks worse the more of them there are, so each entry has to earn its
place.

| Tool | Description |
| --- | --- |
| `list_erablieres` | Lists the maple groves the configured API key can read. Optional `nameContains` filter and `top` (1-100, default 25). |
| `get_erabliere` | Gets a single maple grove by identifier (`erabliereId`, a GUID). |
| `get_alertes` | Lists the alerts of a maple grove: thresholds, recipients, enabled state, last occurrence. |
| `list_capteurs` | Lists the sensors with their **unit**, kind, connectivity and battery level. Required to get a sensor identifier. |
| `get_donnees_capteur` | Summarizes the readings of one sensor over a **mandatory** date range. Never returns a raw dump. |
| `get_dompeux` | Lists the dumping events (tank emptying cycles) with their duration. |
| `get_notes` | Lists the producer's journal, most recent first, with a keyword search. |
| `list_rapports` | Lists the saved reports with their period and aggregates, rows excluded. |
| `get_rapport` | Gets one report with its aggregates and its daily rows. |
| `get_barils` | Lists the barrels closed over a range with their syrup grades. |
| `get_horaire` | Gets the weekly opening hours. |

All of them are annotated `readOnlyHint` / `idempotentHint`, so a client may run them without asking
for confirmation.

> `get_erabliere` is implemented with the OData filter of the list endpoint, because ErabliereAPI
> has no `GET /Erablieres/{id}` route.

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

The MCP client starts this server as a child process, so everything is configured through
environment variables.

| Variable | Required | Description |
| --- | --- | --- |
| `ERABLIEREAPI_URL` | yes | Absolute base url of the API, e.g. `https://erabliereapi.freddycoder.com` or `https://localhost:5001` for a local run. |
| `ERABLIEREAPI_APIKEY` | yes | API key sent in the `X-ErabliereApi-ApiKey` header. Create one in the web application under *Profil -> Clés d'api*. |

The server exits with code `1` and prints what is missing on stderr when a variable is absent or
malformed. Restrict the key to the `GET` verb when creating it: the server never writes.

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

### Claude Code

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

### Claude Desktop

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

```powershell
$env:ERABLIEREAPI_URL = "https://localhost:5001"
$env:ERABLIEREAPI_APIKEY = "<your-api-key>"
npx @modelcontextprotocol/inspector dotnet run --project ErabliereApi.Mcp\ErabliereApi.Mcp.csproj
```

The inspector opens a web page: connect, open the *Tools* tab, then

1. run `list_erablieres` and copy an `id` from `data`;
2. call `get_erabliere`, `get_alertes`, `get_horaire`, `get_notes`, `list_rapports` and `get_barils`
   with it;
3. run `list_capteurs` with the same id and copy a sensor `id`;
4. call `get_donnees_capteur` with both ids and a range covering a day the sensor was reporting,
   for example `startDate=2026-03-12` and `endDate=2026-03-13`; check `data.count`, `data.unit` and
   that `data.serie` holds at most 100 points;
5. call it again over a month to see `truncated` turn true and the `summary` ask for a narrower
   range;
6. call it with `endDate` before `startDate`, and with a missing `startDate`, to check the errors
   come back as readable sentences rather than stack traces.

A local ErabliereAPI can be started with `.\start-light.ps1` at the root of the repository.

## Implementation notes

- **stdout is the JSON-RPC channel.** All logging is forced to stderr in `Program.cs`; writing
  anything on stdout breaks the client.
- **Authentication.** `ErabliereAPI.Proxy` only ships an Azure AD client credentials handler, so
  `Http/ApiKeyHandler.cs` adds the `X-ErabliereApi-ApiKey` header expected by the API's
  `ApiKeyMiddleware`. The retry handler of the proxy project is reused as-is, with a 2 second delay
  instead of the 30 second default, because an MCP client waits synchronously for the tool result.
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
- **`get_dompeux` reads ascending.** The API implements its descending order as a `Reverse()` over an
  unordered EF query, which is not something to depend on, so the tool always asks for `o=c` and says
  in its description that a recent range needs a later `startDate`.
