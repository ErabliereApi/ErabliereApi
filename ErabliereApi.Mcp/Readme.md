# ErabliereApi.Mcp

A [Model Context Protocol](https://modelcontextprotocol.io) server exposing ErabliereAPI to an
MCP client (Claude Code, Claude Desktop, ...). It talks to a running ErabliereAPI instance through
the `ErabliereAPI.Proxy` NuGet client, and communicates with the MCP client over **stdio**.

Phase 1 exposes read-only tools only.

## Tools

| Tool | Description |
| --- | --- |
| `list_erablieres` | Lists the maple groves the configured API key can read. Optional `nameContains` filter and `top` (1-100, default 25). |
| `get_erabliere` | Gets a single maple grove by identifier (`erabliereId`, a GUID). |
| `get_alertes` | Lists the alerts of a maple grove: thresholds, recipients, enabled state, last occurrence. |

All three are annotated `readOnlyHint` / `idempotentHint`, so a client may run them without asking
for confirmation.

> `get_erabliere` is implemented with the OData filter of the list endpoint, because ErabliereAPI
> has no `GET /Erablieres/{id}` route.

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

The inspector opens a web page: connect, open the *Tools* tab, then run `list_erablieres`, copy an
identifier and call `get_erabliere` and `get_alertes` with it.

A local ErabliereAPI can be started with `.\start-light.ps1` at the root of the repository.

## Implementation notes

- **stdout is the JSON-RPC channel.** All logging is forced to stderr in `Program.cs`; writing
  anything on stdout breaks the client.
- **Authentication.** `ErabliereAPI.Proxy` only ships an Azure AD client credentials handler, so
  `Http/ApiKeyHandler.cs` adds the `X-ErabliereApi-ApiKey` header expected by the API's
  `ApiKeyMiddleware`. The retry handler of the proxy project is reused as-is, with a 2 second delay
  instead of the 30 second default, because an MCP client waits synchronously for the tool result.
- **Results are projections.** The proxy DTOs carry a dozen navigation collections that are always
  null on these endpoints. `Models/ErabliereSummary` and `Models/AlerteSummary` only expose the
  meaningful scalar fields, which keeps the tool results small in the model context.
