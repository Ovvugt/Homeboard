# Homeboard

A self-hosted dashboard for your homelab. Pin links to your apps, see at a glance which ones are
up, and add a clock or weather widget for good measure. Inspired by [Homarr](https://homarr.dev/),
rebuilt on a .NET + Vue stack.

Designed for **personal, localhost** use. No auth, no cloud, no Docker — one SQLite file holds
everything.

## Stack

- **Backend** — .NET 10 + ASP.NET Core, [.NET Aspire](https://learn.microsoft.com/dotnet/aspire/)
  for orchestration, SQLite via [Dapper](https://github.com/DapperLib/Dapper),
  [DbUp](https://dbup.github.io/) for migrations, FluentValidation, Serilog
- **Frontend** — Vue 3 + Vite + TypeScript (strict), Pinia, Tailwind CSS,
  [grid-layout-plus](https://github.com/qmhc/grid-layout-plus) for drag-and-drop
- **Tests** — NUnit + `WebApplicationFactory` with ephemeral SQLite (no Testcontainers, no Docker)

## Features

- **Boards & tiles** — drag-and-drop layout, click to edit, drag to a trash zone to delete
- **Status checks** — per-tile HTTP HEAD/GET or TCP pings every 10s, green/red dot in the corner
- **Auto-fetched icons** — `/favicon.ico` → `<link rel="icon">` → Google S2 fallback, BLOB-cached
  in SQLite for 30 days
- **Widgets** — Clock (12h/24h, timezone) and Weather (Open-Meteo, no API key required)
- **Theme** — light by default, with system / dark options, persisted to a cookie

## Project layout

```
Homeboard/
├── Homeboard.Backend/
│   ├── Homeboard.Core/        # SqliteConnectionFactory, DbInitializer, DbUp migrations
│   ├── Homeboard.Boards/      # Boards + tiles + widgets feature
│   ├── Homeboard.Status/      # Per-tile HTTP/TCP poller (BackgroundService)
│   ├── Homeboard.Widgets/     # Open-Meteo weather proxy + cache
│   ├── Homeboard.Icons/       # Favicon resolver + SQLite BLOB cache
│   ├── Homeboard.API/         # Controllers + Program.cs
│   ├── Homeboard.AppHost/     # Aspire orchestration
│   ├── Homeboard.API.Tests/
│   └── Homeboard.Boards.Tests/
└── Homeboard.Frontend/
    ├── src/api/               # Hand-written Axios client per feature
    ├── src/components/        # board/, widgets/, ui/, layout/
    ├── src/stores/            # Pinia (boards, status, theme, editMode)
    └── src/views/             # BoardView, SettingsView
```

The backend follows a vertical-slice modular monolith — each feature project owns its own
entities, repositories, services, and validators, and exposes an `AddXxxFeature()` extension.

## Running it

Prerequisites: **.NET 10 SDK** and **Node 20+**.

```bash
cd Homeboard.Backend
dotnet restore
cd ../Homeboard.Frontend
npm install
```

### Via Aspire (recommended)

```bash
cd Homeboard.Backend
dotnet run --project Homeboard.AppHost
```

The Aspire dashboard opens in your browser; click through to the Frontend resource. Data is stored
at `~/Library/Application Support/Homeboard/homeboard.db` on macOS (LocalApplicationData
elsewhere).

### Running the parts directly

```bash
# Terminal 1 — backend
cd Homeboard.Backend/Homeboard.API
dotnet run

# Terminal 2 — frontend
cd Homeboard.Frontend
npm run dev
```

The Vite dev server runs at <http://localhost:5173/> and proxies `/api/*` to
<http://127.0.0.1:5180/>.

### Tests

```bash
cd Homeboard.Backend
dotnet test
```

Unit tests for validators / slug normalization plus integration tests through
`WebApplicationFactory` against an ephemeral SQLite file. No Docker required.

## Configuration

`Homeboard.API/appsettings.json`:

| Key | Default | Description |
| --- | --- | --- |
| `ConnectionStrings:DefaultConnection` | (Aspire-injected) | SQLite connection string |
| `Status:PollIntervalSeconds` | `10` | How often the status worker scans for stale tiles |
| `Weather:CacheMinutes` | `10` | Open-Meteo response cache TTL |
| `Serilog` | console sink | Logging configuration |

The API binds to `127.0.0.1` only (configured in `launchSettings.json`).
