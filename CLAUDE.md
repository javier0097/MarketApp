# CLAUDE.md

Project context for Claude Code. Read this before working on any task.

## What this is

Inventory and finance management for a small family-owned grocery store
(micromercado). Single developer, ~6 hours per week.

**Where it runs:** locally on one Windows laptop (i7 8th gen, 8 GB RAM, HDD).
The backend runs as a background process on that machine; the UI opens in a
browser at `localhost`. No cloud, no remote access.

**Who uses it:** 2-3 people — the owners, non-technical but comfortable with
business software. Not developers.

**What this means for design decisions:**

- No real concurrency. One writer at a time is fine.
- Modest hardware: avoid anything that adds background services or heavy
  startup cost.
- No multi-tenancy, no public users, no SEO.
- Keep it simple. This is a small internal app, not a platform.

**Current scope (MVP):** create products, list products, record inventory
inbound movements. Nothing else. Point of sale, search, edit, auth, reports
and finances come later — do not build ahead.

## Stack

| Layer | Choice |
| --- | --- |
| Frontend | React + TypeScript + Vite, Mantine |
| Backend | ASP.NET Core (.NET 10), controllers (not minimal APIs) |
| Data | SQLite via EF Core |

One process in production: ASP.NET Core serves both the API and the compiled
React build. One port. Published as a self-contained executable that runs as a
Windows service.

**Deliberately not used:** Next.js (no SSR/SEO need), Electron, SQL Server or
PostgreSQL (they run background services), the Visual Studio React template
(`.esproj`).

## Structure

```
/
├── backend/
│   ├── MarketApp.Api/
│   ├── MarketApp.Api.Tests/   # not created yet
│   └── MarketApp.slnx
├── frontend/              # Vite project, own conventions
├── docs/
└── README.md
```

Backend uses a flat layout: each project sits next to the solution file, no
`src/` or `tests/` folders — with one or two projects they add nesting without
benefit. Frontend follows Vite conventions — tests live next to components,
not in a separate folder.

## Conventions

### Language

Code, comments, commits, PRs, issues and internal docs in **English**.
Everything the user sees — labels, buttons, messages — in **Spanish**.

UI strings are written inline in the components. No translation file or i18n
library: the app has a single language and no plans for another. Users are
addressed formally (usted). Same concept must be worded the same way across
every screen.

### API

- All routes prefixed with `/api`. This is what makes the Vite dev proxy and
  the SPA fallback work.
- REST with controllers. Plural resource names.
- Business operations are not forced into CRUD shape. Recording an inventory
  movement is an operation, not an insert.

### Money and quantities

Money is stored as **integer cents**, never floating point — SQLite has no
native decimal type and rounding errors in a finance app are silent and
cumulative. Convert only at the display boundary, through a single helper.
Never let a raw integer travel through the code without it being clear whether
it is currency or units.

Quantities are integers too (everything is sold by unit for now).

Round as late as possible — only when a number is shown or charged.

### Errors

The API returns a **code**, optional **params** and the offending **field** —
never a display message. The frontend owns the text.

```json
{ "code": "PRODUCT_CODE_DUPLICATE", "params": { "code": "7501234567890" }, "field": "code" }
```

One error code per specific cause. Never a generic `VALIDATION_ERROR` that the
UI turns into "something went wrong". If two situations need different
messages, they are two codes.

Exceptions and logs stay in English. Unexpected failures get a generic code
plus an incident reference that appears in the logs.

### Dates

Stored in **UTC**, converted at display time. `CreatedAt` / `UpdatedAt` are
filled automatically (interceptor or `SaveChanges` override), never set by
hand in endpoints. `UpdatedAt` equals `CreatedAt` on insert.

### UI

Mantine with a custom theme, configured before building screens (font,
primary color, tinted neutrals, spacing scale on a 4/8 rhythm).

The users are in their mid-50s and worked in banking — they handle dense
information fine. The goal is not accessibility for the elderly; it is that
processes are easy to follow and easy to re-derive after weeks without using
them.

- Menu named after tasks, not tables ("Ingresar mercadería", not "Movimientos").
- Use vocabulary they already know from banking: movimiento, saldo, arqueo,
  comprobante.
- Multi-step processes shown as numbered steps with a summary before
  confirming.
- Show the consequence before confirming, with concrete values.
- Empty states explain what goes there and what to do next — they are the
  first-time manual.
- Never an icon-only button. Never an action that only appears on hover.
- Field labels above the input, always visible. Never placeholder-as-label.
- Errors in plain Spanish, next to the problem, not in a toast that fades.
- Consistent placement across screens — users learn by position.
- Product creation must be fast to type: keyboard only, Enter saves and keeps
  focus ready for the next one, no modals. Hundreds of items get loaded this
  way.

### Testing

- Calculation logic gets unit tests from day one — it does not depend on the
  UI, barely changes, and is the most expensive thing to get wrong.
- End-to-end tests only once a flow has stopped changing.
- No coverage targets. Effort proportional to damage: money and stock are high
  risk, cosmetic issues are not.

### Git

- `master` and `develop` are protected against deletion and force push.
- Work branches come from `develop`.
- Always a PR, never a direct push to a protected branch.
- Reference the issue in the PR (`closes #12`).
- Small PRs. A 200-line diff gets reviewed; a 2000-line one gets rubber-stamped.

## Data model

Two tables: `Product` and `InventoryMovement`.

Decisions that are not obvious from the code:

- **Stock is not stored.** It is derived by summing movements. Same for cost.
  Single source of truth; a diff can always be audited.
- **`Product.Code` is unique and required.** If none is supplied, the backend
  generates an internal one (`INT-00001`, letter prefix so it can never
  collide with a real EAN/UPC). Barcodes and internal codes share one column.
- **`InventoryMovement.Amount`** is generic — cost on inbound, income on
  outbound. Not nullable; `0` means no money was involved.
- **Two dates per movement:** when it happened (editable) and when it was
  entered (automatic). Retroactive entry must work — when the app is
  unavailable they write on paper and load it later.
- **Quantity is always positive.** The sign is given by the movement type.
- **Nothing is deleted.** Products are deactivated (`IsActive`, filtered by
  default via a global query filter), movements are voided with a reason.
- **User columns are nullable** until auth exists.
- **Ids are auto-increment integers.**

Postponed on purpose: unit of measure, price history, purchase orders, stored
stock field, adjustment movement type.

## How to work here

- **One task at a time.** Do not implement several checklist items in one go.
- **Explain the approach before writing code**, especially what each piece of
  configuration does and why. Understanding the integration is an explicit
  goal of this project.
- Prefer small, reviewable changes.
- Do not add libraries, abstractions or layers that the current scope does not
  need.

## Commands

```bash
# Backend
dotnet run --project backend/MarketApp.Api

# Frontend (dev)
npm run dev --prefix frontend
```
