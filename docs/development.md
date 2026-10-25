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

**Backend tests are grouped by kind**, one file per class under test, named
after it with `Tests`:

```
MarketApp.Api.Tests/
├── UnitTests/      plain functions, no database   (BarcodeHelperTests.cs)
├── ServiceTests/   services against TestDatabase  (ProductServiceTests.cs)
├── ApiTests/       HTTP requests through ApiFactory (ProductsApiTests.cs)
└── Support/        TestDatabase, ApiFactory
```

The folders are not named after the API's (`Helpers/`, `Services/`): the
naming rules in `.editorconfig` apply by folder, so every class in a
`Services/` folder, tests included, would need the `Service` suffix.

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

## Testing agents

Whoever builds a feature tests it predictably badly: knowing the design, they
follow the paths they know work. So tests and QA are done by two agents in
`.claude/agents/`, each started by its own script in `scripts/`, in a session
of its own, with the task written by the developer.

**Each ticket goes through them in this order:** the test-writer writes the
tests from a specification, the qa-tester tries to break the screen, then the
ticket's "Done when" criteria are checked, and only then the PR. The qa-tester
never gets the acceptance criteria: they would anchor it to what was already
expected. They stay as a separate check, done at the end.

**Neither agent can see the code.**

- The scripts turn auto memory off and pass each agent's own settings file
  (`.claude/test-writer.settings.json`, `.claude/qa-tester.settings.json`),
  which skips every `CLAUDE.md`. The agents' own `omitClaudeMd` is not enough:
  Claude Code ignores it when an agent runs as the main session (`--agent`).
- The test-writer can read, edit and create test files only. A PreToolUse hook
  (`scripts/hooks/test-files-only.mjs`, set up in
  `.claude/test-writer.settings.json`) runs before every Read, Edit and Write
  and lets through only `backend/MarketApp.Api.Tests/` and
  `frontend/src/**/*.test.ts`; any other path is blocked, so it never sees the
  production code. Reading the existing tests lets it extend them and skip what
  is already covered; its prompt treats them as coverage, not as the rules.
- The qa-tester has the browser, web search and file creation; the browser
  tools that run code inside the page are removed, so it can only use the
  screen.
- The test conventions and what any user knows about the store are written in
  the prompts. **If you change the lint rules or how tests are written, update
  `.claude/agents/test-writer.md` too.**

**The qa-tester's browser** is Playwright, set up in `.claude/qa-tester.mcp.json`:
Microsoft Edge, a profile kept in memory, and only the QA app allowed. The
script passes `--strict-mcp-config`, so no other MCP server (such as Notion)
loads. The snapshots it saves go to `.playwright-mcp/`, which is git-ignored.

**The QA environment** (`appsettings.QA.json`) is not Production, so it never
takes the single-instance mutex or opens the browser by itself. It runs without
the launch profile, which would set the Development environment; the published
app never uses `launchSettings.json` either. Its database keeps data from
earlier sessions, like a real QA environment: existing data shows bugs an empty
database hides. Each finding in a report records the data that mattered.
