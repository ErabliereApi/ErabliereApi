# ErabliereModel — the data model (`ErabliereApi.Donnees`)

Entities and request/response shapes. No behaviour, no data access.

## Layout

| Path | Contents |
|---|---|
| `*.cs` at the root | 42 entities, **one type per file**: `Erabliere`, `Capteur`, `DonneeCapteur`, `Alerte`, `Baril`, `Entaille`, `LigneTubelure`, `Abonnement`, … |
| `Action/Post/`, `Put/`, `Patch/`, `Get/`, `Delete/`, `NonHttp/` | DTOs, named verb + entity: `PostCapteur`, `PutLigneTubelure`, `PostEntaille`. |
| `Ownable/` | `IOwnable`, `IErabliereOwnable`, `ILevelTwoOwnable`, `IUserOwnable` — how authorization finds the owning érablière. |
| `Interfaces/` | `IIdentifiable`, `IDatesInfo`, `ILocalizable`, `IIsPublic`, `IAltitude`, `IAlerteTexte`, `IDonneeTexte`, `IFileStorage`. |
| `Contantes/` | `Specifications`, `TypeLigneTubelure`. *(folder name is misspelled in the repo — leave it)* |
| `Generic/` | `Pair`. |

## `Erabliere` is the root

Most entities are owned by one érablière, directly (`IErabliereOwnable`) or one level down
(`ILevelTwoOwnable`). Implement the right marker on a new entity — authorization and the ownership
filters depend on it.

## The entity / DTO split is a security boundary

Entities carry navigation properties. DTOs must not.

A write DTO holds **scalar fields and foreign-key ids only**. If it exposes a navigation property,
an attacker can use it to reach rows in an érablière they don't own — EF traverses the graph on
`AddAsync` / `Update`. `ErabliereApi.Test/WriteEndpointsBindDtoNotEntityTest.cs` fails the build when
a controller binds an entity, but nothing stops you putting a navigation property on a DTO. Don't.

→ [.claude/_shared/write-endpoint-dto.md](../.claude/_shared/write-endpoint-dto.md)

## Adding an entity

1. One file at the root, French name, right `Ownable` marker.
2. `DbSet<>` on `ErabliereDbContext` and a configuration in
   `ErabliereApi/Depot/Sql/EntityConfiguration/`.
3. Write DTOs in `Action/Post/` and `Action/Put/`.
4. Migration — see [.claude/workflows/feature-slice.md](../.claude/workflows/feature-slice.md) step 3.

## Consumers

Changing an entity or DTO ripples into `ErabliereApi/Controllers/`, the NSwag-generated
`ErabliereAPI.Proxy/`, the MCP projections in `ErabliereApi.Mcp/Models/`, and the TypeScript
interfaces in `ErabliereIU/src/model/`. None of those update themselves.
