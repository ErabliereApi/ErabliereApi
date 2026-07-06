# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

ErabliereIU is the Angular (v22) web front-end for ErabliereAPI, a maple-syrup-production monitoring platform. It is a standalone-component Angular app (no NgModules) that talks to the ErabliereAPI backend over HTTP/OData.

## Commands

- `npm start` — dev server (`ng serve`) over **HTTPS** on `https://localhost:4200` using the cert in `localconfig/` (see below). MSAL redirect auth requires HTTPS, so plain HTTP won't work.
- `npm run build` / `npm run build:prod` — dev / production build to `dist/ErabliereIU`.
- `npm run lint` — ESLint via `ng lint`.
- `npx cypress run` (or `npm run cypress:run`) — run Cypress E2E tests headless. `npm run cypress:open` for interactive.
- Run a single Cypress spec: `npx cypress run --spec "cypress/integration/notes.spec.ts"`.
- `ng extract-i18n --format=json --out-file=src/assets/i18n/ca-fr.json` — regenerate i18n translation files.

There is **no unit-test runner** (no Karma/Jasmine test target). All automated tests are Cypress E2E specs in `cypress/integration/`, which run against a live dev server at `https://localhost:4200`.

### Local HTTPS cert

`ng serve` is configured (`angular.json`) to use `localconfig/localhost.crt` and `localconfig/localhost.key`. These must exist locally. Requires Node 26 for Angular 22 compatibility.

## Runtime configuration (important)

Config is **loaded at runtime**, not baked in at build time. On startup, `provideAppInitializer` calls `EnvironmentService.loadConfig()`, which `fetch`es `/assets/config/oauth-oidc.json` and populates `apiUrl`, `clientId`, `tenantId`, `scopes`, `authEnable`, etc. See `src/assets/config/oauth-oidc*.json` for example shapes (Azure AD, IdentityServer, no-auth, docker). To change the target API or auth in a given environment, edit that JSON — no rebuild needed.

## Architecture

### Authentication is pluggable

Auth is abstracted behind `IAuthorisationSerivce` and resolved at runtime by `AuthorisationFactoryService` (`src/core/authorisation/`):
- `authEnable: false` → `AuthorisationBypassService` (no auth, for local/dev).
- `authEnable: true` + a `tenantId` → `AzureADAuthorisationService` backed by MSAL (`@azure/msal-angular` / Entra ID).

MSAL is wired in `src/app/app.config.ts` (`MSALInstanceFactory`, `MSALInterceptorConfigFactory`) using the runtime `EnvironmentService`. Never call MSAL directly from features — go through `AuthorisationFactoryService.getAuthorisationService()`. Route guards (`src/app/guard/`) do the same: `AdminGuard` checks the `administrateur` role, `AuthenticatedUserGard` checks login.

### Central data access: `ErabliereApi` service

`src/core/erabliereapi.service.ts` is the single large service wrapping all backend calls. It builds **OData** query strings by hand (`$filter`, `$expand`, `$orderby`) against `EnvironmentService.apiUrl` and attaches auth headers from the resolved auth service. The API uses custom `x-ddr` / `x-dde` HTTP headers to minimize transferred data (delta ranges) — search the repo for these when touching data-fetch optimization. New backend endpoints are added as methods here.

### Feature areas (`src/app/`)

Routing lives in `src/app/app.routes.ts`. Top-level view shells each have their own nav bar and children:
- `client-view/` — main user-facing app (érablières dashboard, capteurs, alertes, notes, rapports, appareils, documentation, profil). The default `''` route redirects to `e` (érablière selection); most routes are keyed by `e/:idErabliereSelectionee/...`.
- `admin-view/` — admin area under `/a`, protected by `AdminGuard` (customers, érablières, API keys, chirpstack, hologram, ipinfos, connected platforms).
- `ai-view/` + `generic/erabliereai/` — ErabliereAI chat/image features (`/ai`, plus public conversation `/ai/public/:conversationId`).
- `map-view/` — érablières map using `mapbox-gl` (`/map`).
- `generic/` — shared components (forms, modal, pagination, customer, directives, erabliereai widgets).
- `model/` — TypeScript interfaces for API DTOs (one file per type).

### UI stack

- Bootstrap 5 (global CSS/JS from `node_modules`, configured in `angular.json`) + `bootstrap-icons`.
- Charts via `ng2-charts` / `chart.js` with the `chartjs-adapter-date-fns` time adapter (registered globally in `app.config.ts`).
- `ngx-mask` for input masking.
- Component selectors use the `app` prefix (kebab-case elements, camelCase attribute directives — enforced by `.eslintrc.json`).

### i18n

Angular built-in i18n. Source locale is **French** (`fr`); English is provided via `src/assets/i18n/en.json` (see `angular.json` `i18n` block). French is the default language of the app and much of the codebase (identifiers, route paths like `érablieres`, `capteurs`).

## Testing against the real backend

To exercise the `x-ddr`/`x-dde` optimization and CORS-affected headers, build into the API's static root and run ErabliereAPI itself:
`ng build --output-path="..\ErabliereApi\wwwroot\."` then start the ErabliereApi project (same-origin avoids CORS stripping the custom headers).
