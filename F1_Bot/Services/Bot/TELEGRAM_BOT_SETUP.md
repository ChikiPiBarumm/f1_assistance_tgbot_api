# Telegram Bot Setup Guide

## Getting Your Bot Token

1. Open Telegram and search for **@BotFather**
2. Send `/newbot` command
3. Follow the instructions to name your bot
4. BotFather will give you a **Bot Token** (looks like: `123456789:ABCdefGHIjklMNOpqrsTUVwxyz`)

## Configuration

1. Open `appsettings.json` or `appsettings.Development.json`
2. Replace `YOUR_BOT_TOKEN_HERE` with your actual bot token:

```json
{
  "TelegramBot": {
    "BotToken": "123456789:ABCdefGHIjklMNOpqrsTUVwxyz"
  }
}
```

## Running the Bot

1. Start your application (F5 in Rider or `dotnet run`)
2. The bot will automatically start with **long polling**
3. Find your bot on Telegram (search for the username you gave it)
4. Send `/start` to test it!

## Available Commands

- `/start` - Welcome message
- `/next_race` or `/nextrace` - Get the next upcoming race
- `/standings`, `/driver_standings`, or `/driverstandings` - Driver championship standings
- `/team_standings` or `/teamstandings` - Constructor championship standings
- `/last_race`, `/lastrace`, or `/results` - Results from the last race
- `/help` - Show help message

## Architecture Notes

The bot is designed with a clean architecture:

- **Business Logic**: `TelegramBotCommandRouter` handles all commands (shared by polling and webhooks)
- **Message Sending**: `TelegramBotMessageSender` provides retry logic and timeout handling for all messages
- **Connection Method**: `TelegramBotPollingService` implements long polling (can be swapped for webhooks later)
- **Services**: Bot uses your existing F1 services (`ICalendarService`, `IStandingsService`, `IRaceResultsService`)

The bot uses `IHttpClientFactory` with optimized connection pooling to prevent timeout issues.

To switch from polling to webhooks later, you only need to:
1. Create `TelegramBotWebhookService : ITelegramBotService`
2. Change the registration in `Program.cs`
3. No changes needed to command handlers or message sender!
