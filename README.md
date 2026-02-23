# F1 Assistance Bot

A **Telegram bot** and **REST API** for Formula 1 race calendar, standings, session schedules, and race results. Built with .NET 10 and powered by the [OpenF1 API](https://api.openf1.org).

---

## Features

### Telegram Bot
- **Calendar** — Full season calendar with round selection; tap a round for details, schedule, and results
- **Next race** — Upcoming race info, session times, and (when available) results
- **Last race** — Quick access to the most recent race details and results
- **Standings** — Driver and constructor championship standings; view by round or switch season
- **Race details** — Session schedule, race info, and results for any round
- **Season mode** — Switch between current season and past years (history mode) for historical data
- **Inline keyboards** — Navigate via buttons; no need to type commands for common actions

### REST API
- **Races** — `GET /api/races`, `GET /api/races/{round}`, `GET /api/races/next`
- **Sessions** — `GET /api/races/{round}/sessions`
- **Results** — `GET /api/races/{round}/results` or `GET /api/races/latest/results`
- **Standings** — `GET /api/standings/drivers`, `GET /api/standings/teams`

In Development, Swagger UI is available at `/swagger`.

---

## Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- A [Telegram Bot Token](https://core.telegram.org/bots#botfather) (for the bot)

---

## Quick Start

### 1. Clone and build

```bash
git clone <repo-url>
cd F1_Bot
dotnet build
```

### 2. Configure the bot

Create or edit `F1_Bot/appsettings.json` (or `appsettings.Development.json`) and set your Telegram bot token:

```json
{
  "TelegramBot": {
    "BotToken": "YOUR_BOT_TOKEN_HERE"
  }
}
```

To get a token: open Telegram → search **@BotFather** → send `/newbot` and follow the steps.

### 3. Run

```bash
cd F1_Bot
dotnet run
```

The app starts as a web host with **long polling** for the Telegram bot. In Development, open `https://localhost:<port>/swagger` for the API docs.

---

## Project Structure

```
F1_Bot/
├── F1_Bot/                    # Main project
│   ├── Domain/                # Models, constants
│   │   ├── Constants/         # RaceStatus, OpenF1SessionName, SeasonConstants
│   │   └── Models/            # Race, RaceDetails, Session, Standings, etc.
│   ├── Application/           # Business logic
│   │   ├── Interfaces/        # ICalendarService, IStandingsService, ...
│   │   ├── Mapping/           # OpenF1 DTO → domain mappers
│   │   └── Services/          # Calendar, Standings, RaceResults, Session, RaceDetails
│   ├── Infrastructure/        # External APIs
│   │   └── OpenF1/            # OpenF1 API client (api.openf1.org)
│   └── Presentation/
│       ├── Bot/               # Telegram bot
│       │   ├── Handlers/      # Start, Calendar, RaceDetails, Standings, Results, Mode
│       │   ├── TelegramBotCommandRouter.cs
│       │   ├── PollingService.cs
│       │   └── MessageSender.cs
│       └── Api/               # REST API
│           └── Controllers/   # RacesController, StandingsController
├── F1_Bot.sln
└── README.md
```

**Architecture:** Clean separation of Domain, Application, Infrastructure, and Presentation. The bot and API share the same application services; the bot uses long polling by default and can be switched to webhooks by replacing the hosted service.

---

## Bot Commands

| Command | Description |
|--------|-------------|
| `/start` | Welcome message and quick-action buttons |
| `/help` | Help text and main buttons |
| `/calendar` | Full race calendar for the selected season |
| `/next_race`, `/nextrace` | Next upcoming race |
| `/last_race`, `/lastrace` | Last race details and results |
| `/results [round]` | Race results (last race if round omitted) |
| `/race [round]`, `/race_info [round]` | Race details and sessions |
| `/schedule [round]`, `/sessions [round]` | Session schedule for a round |
| `/standings` | Driver/constructor standings choice |
| `/driver_standings`, `/driverstandings` | Driver championship standings |
| `/team_standings`, `/teamstandings` | Constructor championship standings |
| `/mode`, `/status` | Show/change season (current vs history year) |
| `/current`, `/current_mode` | Switch to current season |
| `/history [year]`, `/history_mode [year]` | Switch to history mode for a given year |

---

## Configuration

| Key | Description |
|-----|-------------|
| `TelegramBot:BotToken` | **Required.** Telegram bot token from BotFather. |
| `Logging:LogLevel` | Log level (e.g. `Information`, `Warning`). |
| `AllowedHosts` | Allowed host headers (`*` for development). |

Data is fetched from **https://api.openf1.org**; no API key is required for OpenF1.

---

## Documentation

- **Bot setup:** See [TELEGRAM_BOT_SETUP.md](F1_Bot/Presentation/Bot/TELEGRAM_BOT_SETUP.md) in the Bot folder for token setup, commands, and architecture notes.

---

## License

See repository license file (if present).
