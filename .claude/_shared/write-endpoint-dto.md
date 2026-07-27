# No entity binding on write endpoints — REQUIRED

Applies to: every `POST` / `PUT` / `PATCH` action in `ErabliereApi/Controllers/`.
Enforced by: `ErabliereApi.Test/WriteEndpointsBindDtoNotEntityTest.cs` (build fails on violation).

## The rule

Never bind an EF entity from `ErabliereApi.Donnees` (`Erabliere`, `Capteur`, `Arbre`, `Entaille`,
`LigneTubelure`, …) directly as the body parameter of a write action.

## Why

EF Core traverses the object graph on `AddAsync` / `Update`. An authenticated attacker can populate
an entity's **navigation properties** (`Erabliere`, `Entailles`, `Arbre`, …) to create or modify rows
in an érablière they don't own — bypassing `ValiderOwnership` and the sibling controllers' checks.
This is what happened on the tubelure feature.

`ValiderOwnership("id")` and the `id != body.IdErabliere` guard only protect the **root** entity.
They never protect nested children.

## What to do instead

- **Bind a dedicated DTO** from `ErabliereModel/Action/Post/` or `Action/Put/` holding only scalar
  fields and foreign-key ids — **no navigation properties**. See `PostEntaille`, `PutLigneTubelure`,
  `PostCapteur`.
- **POST** — build the entity explicitly field-by-field before `AddAsync`, or use `MapTo<TEntity>()`
  (copies by name, so it cannot set navigations the DTO doesn't have).
- **PUT / PATCH** — load the existing entity with `FindAsync`, verify it belongs to the route's
  érablière (`entity.IdErabliere != id → NotFound()`), then assign only the allowed fields.
  Do **not** call `_depot.Update(bodyEntity)` — that attaches the whole graph.
- **Validate every client-supplied FK** (`IdArbre`, `IdLigneTubelure`, …) belongs to the same
  érablière before saving.

## The enforcing tests

| Test | File | What it does |
|---|---|---|
| `AucunEndpointDEcriture_NeLieUneEntite_HorsExceptionsConnues` | `ErabliereApi.Test/WriteEndpointsBindDtoNotEntityTest.cs` | Reflects over every controller; fails if a write action binds a type tracked as a `DbSet<>` on `ErabliereDbContext`. |
| `ExceptionsConnues_NeContiennentPasDEntreePerimee` | same file | Fails if an entry in `ExceptionsConnues` no longer matches a real violation — so removing a violation *forces* removing its exception. |
| `OverPostingMigrationTest` | `ErabliereApi.Integration.Test/OverPostingMigrationTest.cs` | End-to-end proof that the over-posting path is closed. |

## The grandfathered exceptions

Two admin-only endpoints stay in `ExceptionsConnues` as accepted tech debt: **Chirpstack**
server-config create/edit, and **IpInfo** import. They're reachable only by administrators, so the
multi-tenant risk is low.

**Do not add to that list.** Migrate those two to DTOs when you next touch them.
