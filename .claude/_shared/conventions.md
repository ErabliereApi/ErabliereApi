# Conventions

Cross-cutting rules that hold in every project of the solution.

## French first

Identifiers, routes, comments, and docs are in **French** — `ErablieresController`, `DonneeCapteur`,
`ValiderOwnership`, `AucunEndpointDEcriture_NeLieUneEntite`. Follow it when adding code; do not
"normalize" existing French names to English.

Accented route segments are real and intentional (`érablieres`, `capteurs`). The Angular app's source
locale is `fr`; English ships as a translation (`ErabliereIU/src/assets/i18n/en.json`).

## One entity, one file

`ErabliereModel/` holds one type per file, 42 of them at the root. `Erabliere` is the root of the
data hierarchy — most entities are owned by one, through the marker interfaces in
`ErabliereModel/Ownable/` (`IErabliereOwnable`, `ILevelTwoOwnable`, `IUserOwnable`, `IOwnable`).

## Write DTOs live apart from entities

Request/response shapes go in `ErabliereModel/Action/{Get,Post,Put,Patch,Delete,NonHttp}/`, named for
the verb plus the entity: `PostCapteur`, `PutLigneTubelure`. This split is what makes the
[no-entity-binding rule](write-endpoint-dto.md) enforceable.

## Registration goes through Extensions

`Program.cs` → `Startup.cs`, which delegates to extension methods in `ErabliereApi/Extensions/`
(`AddErabliereApiControllers`, `AddErabliereAPIAuthentication`, `AddDatabase`, `AddHttpClients`, …).
Wire new services there, **not** inline in `Startup.cs`.

## OData is the query surface

Controllers expose `$filter`, `$expand`, `$orderby`, consumed by the Angular app's
`ErabliereIU/src/core/erabliereapi.service.ts`, which builds those query strings by hand. Guard
queryable endpoints with `SecureEnableQueryAttribute` rather than a bare `[EnableQuery]`.

## The x-ddr / x-dde delta headers

Custom request headers minimize transferred sensor data. CORS strips them cross-origin, so anything
touching data-fetch optimization must be tested **same-origin**: build the UI into the API's static
root and run the API alone.

```powershell
cd ErabliereIU
ng build --output-path="..\ErabliereApi\wwwroot\."
```

## Generated code is not hand-edited

`ErabliereAPI.Proxy/` is NSwag-generated from the OpenAPI document (`GenerateProxy.ps1`, see its
Readme). Regenerate it; never patch `ErabliereAPIClient.cs` or `ErabliereAPIContract.cs` by hand.

## Project tracking

Azure DevOps: https://dev.azure.com/freddycoder/ErabliereAPI
