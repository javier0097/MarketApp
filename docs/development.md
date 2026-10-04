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
