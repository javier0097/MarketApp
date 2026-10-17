# Development

The rules themselves live in `.editorconfig`, `frontend/eslint.config.js`
and the hooks in `.githooks/`.

**Formatting never blocks; lint blocks only on push.** Anything a tool can
fix by itself is fixed automatically, in the pre-commit hook. Rules that need
judgment are checked by the pre-push hook.

**Imports are never sorted automatically.** A sorting plugin was tried and
silently changed the order of CSS imports, which changes which styles win.
Do not enable editor actions that sort imports on save (such as
`source.organizeImports` in VS Code): no hook would catch the reordering.

**Lint rules apply to every backend project.** The `Lint` block that turns
the `.editorconfig` rules into build errors lives in
`backend/Directory.Build.props`, which MSBuild imports into every project
under `backend/`. While it lived in the API's `.csproj`, the test project
compiled without it and its lint errors passed the push.

## Tests

How to run them is in the [root README](../README.md#tests).

**xUnit v3 needs the new test platform.** xUnit v3 runs on
Microsoft.Testing.Platform, and on the .NET 10 SDK `dotnet test` only runs it
when `global.json` (repo root) opts in. That is also why `dotnet test` takes
`--solution` or `--project` before the path. Each xUnit v3 test project is an
executable (`OutputType` `Exe`).

**Service tests use `TestDatabase`** (`MarketApp.Api.Tests/Support/`): SQLite
in memory, the same engine as production, with the real migrations applied.
An in-memory database only lives while its connection is open, so each test
creates its own and gets a clean, isolated database. `CreateContext()` returns
a new context on each call: checking with a different context than the one
that saved reads what actually reached the database, not what the first
context keeps in memory.

**API tests use `ApiFactory`** (same folder): it runs the real `Program.cs`
in memory, and `CreateClient()` sends HTTP requests through the whole
pipeline (validation, controllers, `GlobalExceptionHandler`, JSON). It
replaces the database with an in-memory one per factory and runs in
Development, so the Production-only startup (single-instance mutex, opening
the browser, `C:\ProgramData`) never happens.

**Only plain functions are tested in the frontend.** Component tests (jsdom,
Testing Library) and end-to-end tests are left out while the screens keep
changing; whether to add them is tracked in #9.
