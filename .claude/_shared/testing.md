# Testing

Four test projects, all xUnit. Pick by what you're proving.

| Project | Use it for | Notable contents |
|---|---|---|
| `ErabliereApi.Test` | Controller and extension unit tests, plus the architecture guards. | `WriteEndpointsBindDtoNotEntityTest`, `ValiderIPRulesAttributeTest`, `EqualityComparer/`, `Extension/` |
| `ErabliereApi.Integration.Test` | In-memory `WebApplicationFactory` + AngleSharp, end-to-end through the pipeline. | `ApplicationFactory/`, `StripeWebhookJson/` fixtures, `OverPostingMigrationTest`, `TubelureTest`, `OdataHttpQueryTest` |
| `ErabliereApi.Mcp.Test` | The MCP server and the `ErabliereApi.Mcp.Tools` library: tools, transports, plan gate, response budget. | `HttpTransportTest`, `PlanGateTest`, `ToolResponseTest`, `ToolDiscoveryTest` |
| `ErabliereApi.Test.Autofixture` | Shared fixtures consumed by the others — not a test suite itself. | `AutoApiData`, `ErabliereFixture`, `DbContextExtension` |

The Angular app has **no unit-test runner**. All its automated tests are Cypress E2E specs in
`ErabliereIU/cypress/integration/`, run against a live dev server — see `ErabliereIU/CLAUDE.md`.

## Commands

```powershell
dotnet test                                        # everything
dotnet test ErabliereApi.Test                      # one project
dotnet test --filter "FullyQualifiedName~MyTestName"   # one test
.\start-code-coverage-report.ps1                   # + HTML report in coveragereport/ (needs reportgenerator)
```

## Guards that will fail your build

- **`WriteEndpointsBindDtoNotEntityTest`** — reflects over every controller and rejects any
  `POST`/`PUT`/`PATCH` binding an EF entity. Read [write-endpoint-dto.md](write-endpoint-dto.md)
  before adding a write endpoint; that file explains the failure and the fix.
- Its sibling test also fails when an entry in `ExceptionsConnues` has gone stale — so fixing a
  violation means deleting its exception in the same change.

## CI

GitHub Actions in `.github/workflows/`:

| Workflow | Job |
|---|---|
| `api-test-demo.yml` | Runs the tests |
| `api-image.yml` | Builds the API docker image |
| `codeql-analysis.yml` | Security scanning — recent `fix/code-scanning-*` branches came from here |
| `proxy-publish.yaml` | Publishes the NuGet proxy package |
| `extraireinfohmi-image.yml` | Builds the ExtraireInfoHMI image |

The MCP docker image runs its unit **and** integration tests during the build, so a broken MCP test
breaks `docker build`.
