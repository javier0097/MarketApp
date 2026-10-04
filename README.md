# MarketApp

Inventory and finance management for Toma&Ve, a small family-owned grocery
store. It runs locally on a single Windows laptop: one executable serves both
the API and the web UI, which opens in the browser.

- **Backend:** ASP.NET Core (.NET 10), EF Core, SQLite
- **Frontend:** React, TypeScript, Vite, Mantine

## Requirements for development

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22](https://nodejs.org/) or later
- `dotnet ef`, only to create migrations:
  ```bash
  dotnet tool install --global dotnet-ef --version 10.0.12
  ```

## First run after cloning

```bash
npm ci --prefix frontend
dotnet build backend/MarketApp.slnx
```

The first backend build also activates the git hooks (`.githooks/`).

## Development

Run the backend and the frontend in parallel:

| | Command | Editor | URL |
| --- | --- | --- | --- |
| Backend | `dotnet run --project backend/MarketApp.Api` | F5 in Visual Studio | http://localhost:5120 |
| Frontend | `npm run dev --prefix frontend` | F5 in VS Code (open `frontend/`) | http://localhost:5173 |

Use the app at http://localhost:5173: Vite reloads changes instantly and
forwards `/api` requests to the backend.

The development database is `backend/MarketApp.Api/marketapp.db`. It is
created and migrated automatically when the backend starts.

### Migrations

After changing an entity, create a migration:

```bash
dotnet ef migrations add <Name> --project backend/MarketApp.Api --output-dir Data/Migrations
```

It is applied automatically the next time the backend starts, in development
and on the store laptop.

### Git hooks

- **pre-commit** formats the backend and the frontend and re-stages the
  files that were already staged. It never blocks a commit for formatting.
- **pre-push** runs the backend lint rules, the TypeScript type check and
  ESLint, and blocks the push if any of them fails.

## Publish

From Visual Studio: **Publish** with the `FolderProfile` profile. From a
terminal:

```bash
dotnet publish backend/MarketApp.Api -c Release -o publish
```

Both produce the `publish/` folder: a self-contained Windows executable
(no .NET installation needed) with the frontend build included.

## Install on the store laptop (first time)

1. Zip the contents of `publish/` and copy it with a USB drive. Files
   downloaded from the internet trigger the Windows SmartScreen warning.
2. Extract everything into `C:\Program Files\MarketApp` (requires
   administrator permission).
3. Right-click `MarketApp.Api.exe` → *Send to* → *Desktop (create
   shortcut)*, and rename the shortcut to "Toma&Ve".
4. Double-click the shortcut. The app starts in the background and opens
   the browser. The database is created at
   `C:\ProgramData\MarketApp\marketapp.db`.

## Update the store laptop

1. Publish the new version.
2. On the laptop, stop the app: Task Manager (`Ctrl+Shift+Esc`) →
   *MarketApp.Api* → *End task*, or restart the laptop.
3. Replace the contents of `C:\Program Files\MarketApp` with the new
   `publish/` folder.
4. Open the app. If the version brings database changes, a backup is saved
   first in `C:\ProgramData\MarketApp\backups\`.
