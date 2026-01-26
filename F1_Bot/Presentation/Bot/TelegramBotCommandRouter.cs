using System.Linq;
using F1_Bot.Services;
using F1_Bot.Domain.Models;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot;

public class TelegramBotCommandRouter
{
    private readonly TelegramBotMessageSender _messageSender;
    private readonly ICalendarService _calendarService;
    private readonly IStandingsService _standingsService;
    private readonly IRaceResultsService _raceResultsService;
    private readonly IRaceDetailsService _raceDetailsService;
    private readonly ISessionService _sessionService;
    private readonly IUserStateService _userStateService;
    private readonly ILogger<TelegramBotCommandRouter> _logger;

    public TelegramBotCommandRouter(
        TelegramBotMessageSender messageSender,
        ICalendarService calendarService,
        IStandingsService standingsService,
        IRaceResultsService raceResultsService,
        IRaceDetailsService raceDetailsService,
        ISessionService sessionService,
        IUserStateService userStateService,
        ILogger<TelegramBotCommandRouter> logger)
    {
        _messageSender = messageSender;
        _calendarService = calendarService;
        _standingsService = standingsService;
        _raceResultsService = raceResultsService;
        _raceDetailsService = raceDetailsService;
        _sessionService = sessionService;
        _userStateService = userStateService;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.Message is not { } message)
        {
            return;
        }

        if (message.Text is not { } messageText)
        {
            return;
        }

        _logger.LogDebug("Received message: {Text} from user {UserId}", messageText, message.From?.Id);

        try
        {
            var parts = messageText.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();
            var arguments = parts.Skip(1).ToArray();

            using var commandCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, commandCts.Token);
            
            switch (command)
            {
                case "/start":
                    await HandleStartCommandAsync(message, linkedCts.Token);
                    break;

                case "/history":
                case "/history_mode":
                    await HandleHistoryCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/current":
                case "/current_mode":
                    await HandleCurrentCommandAsync(message, linkedCts.Token);
                    break;

                case "/mode":
                case "/status":
                    await HandleModeCommandAsync(message, linkedCts.Token);
                    break;

                case "/next_race":
                case "/nextrace":
                    await HandleNextRaceCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/driver_standings":
                case "/driverstandings":
                case "/standings":
                    await HandleDriverStandingsCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/team_standings":
                case "/teamstandings":
                    await HandleTeamStandingsCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/last_race":
                case "/lastrace":
                    await HandleLastRaceResultsCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/results":
                    await HandleResultsCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/race":
                case "/race_info":
                    await HandleRaceDetailsCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/schedule":
                case "/sessions":
                    await HandleScheduleCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/calendar":
                    await HandleCalendarCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/help":
                    await HandleHelpCommandAsync(message, linkedCts.Token);
                    break;

                default:
                    await HandleUnknownCommandAsync(message, linkedCts.Token);
                    break;
            }
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout handling message from user {UserId}", message.From?.Id);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error handling message from user {UserId}", message.From?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling message from user {UserId}, command: {Command}", message.From?.Id, messageText?.Split(' ')[0]);
            
            try
            {
                await _messageSender.SendMessageAsync(
                    message.Chat.Id,
                    "Sorry, an error occurred while processing your request. Please try again later.",
                    cancellationToken: cancellationToken);
            }
            catch
            {
            }
        }
    }

    private async Task HandleStartCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var welcomeText = @"🏎️ Welcome to F1 Assistance Bot!

I can help you with Formula 1 information for current and historical seasons!

Mode Switching:
/history [year] - View historical season data
/current - Return to current season
/mode - Check your current mode

Quick Commands:
/next_race - Next upcoming race
/calendar - Full race calendar
/standings - Driver standings
/team_standings - Team standings
/results - Last race results

Use /help for detailed command information!";

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                welcomeText,
                cancellationToken: cancellationToken);
        }
        catch
        {
            throw;
        }
    }

    private async Task HandleNextRaceCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, _) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);
        var nextRace = await _calendarService.GetNextRaceAsync(year);

        if (nextRace == null)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No upcoming race found.",
                cancellationToken: cancellationToken);
            return;
        }

        var raceText = $"🏁 **{nextRace.Name}**\n\n" +
                      $"📍 {nextRace.CircuitName}\n" +
                      $"🌍 {nextRace.City}, {nextRace.Country}\n" +
                      $"📅 {nextRace.Date:dd MMMM yyyy}\n" +
                      $"🔢 Round {nextRace.RoundNumber}";
        
        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                raceText,
                cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout sending message to Telegram API");
            throw;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error sending message to Telegram API");
            throw;
        }
    }

    private async Task HandleDriverStandingsCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);
        var standings = await _standingsService.GetDriverStandingsAsync(year, round);

        if (standings.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No driver standings available.",
                cancellationToken: cancellationToken);
            return;
        }

        var standingsText = "🏆 Driver Championship Standings\n\n";
        foreach (var standing in standings.Take(10))
        {
            standingsText += $"{standing.Position}. {standing.DriverName} ({standing.TeamName}) - {standing.Points} pts\n";
        }

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                standingsText,
                cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout sending driver standings to user {UserId}", message.From?.Id);
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Request timed out. Please try again later.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending driver standings to user {UserId}", message.From?.Id);
            throw;
        }
    }

    private async Task HandleTeamStandingsCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);
        var standings = await _standingsService.GetTeamStandingsAsync(year, round);

        if (standings.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No team standings available.",
                cancellationToken: cancellationToken);
            return;
        }

        var standingsText = "🏆 Constructor Championship Standings\n\n";
        foreach (var standing in standings.Take(10))
        {
            standingsText += $"{standing.Position}. {standing.TeamName} - {standing.Points} pts\n";
        }

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                standingsText,
                cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout sending team standings to user {UserId}", message.From?.Id);
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Request timed out. Please try again later.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending team standings to user {UserId}", message.From?.Id);
            throw;
        }
    }

    private async Task HandleLastRaceResultsCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, _) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);
        var results = await _raceResultsService.GetLastRaceResultsAsync(year);

        if (results.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No race results available.",
                cancellationToken: cancellationToken);
            return;
        }

        var resultsText = "🏁 Last Race Results\n\n";
        foreach (var result in results.Take(10))
        {
            resultsText += $"P{result.Position}. {result.DriverName} ({result.TeamName}) - {result.Points} pts\n";
        }

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                resultsText,
                cancellationToken: cancellationToken);
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogError(ex, "Timeout sending race results to user {UserId}", message.From?.Id);
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Request timed out. Please try again later.",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending race results to user {UserId}", message.From?.Id);
            throw;
        }
    }

    private async Task HandleHelpCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var helpText = @"📖 Available Commands:

Mode Switching:
/history [year] - Switch to history mode for a year
/current - Switch back to current season mode
/mode - Show current mode and year

Current Season Commands:
/next_race - Get the next upcoming race
/calendar - Full race calendar
/race [round] - Get detailed race info
/schedule [round] - Get session schedule
/standings [year] [round] - Driver standings
/team_standings [year] [round] - Team standings
/results [round] [year] - Race results

Examples:
/standings - Current season standings
/standings 2023 - 2023 final standings
/standings 2023 5 - 2023 standings after round 5
/race 5 2023 - Round 5 of 2023

/help - Show this help message";

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                helpText,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending help message to user {UserId}", message.From?.Id);
            throw;
        }
    }

    private async Task HandleRaceDetailsCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);

        if (!round.HasValue)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Please provide a valid round number.\nExample: /race 5\nOr: /race 5 2023",
                cancellationToken: cancellationToken);
            return;
        }

        var race = await _raceDetailsService.GetRaceByRoundAsync(round.Value, year);

        if (race == null)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                $"❌ Race not found for round {round}.",
                cancellationToken: cancellationToken);
            return;
        }

        var raceText = $"🏁 {race.Name}\n\n" +
                      $"📍 {race.CircuitName}\n" +
                      $"🌍 {race.City}, {race.Country}\n" +
                      $"📅 {race.Date:dd MMMM yyyy}\n" +
                      $"🔢 Round {race.RoundNumber}\n" +
                      $"📊 Status: {race.Status}";

        if (race.Sessions.Count > 0)
        {
            raceText += "\n\n📋 Sessions:";
            foreach (var session in race.Sessions.OrderBy(s => s.StartTime))
            {
                var timeStr = session.StartTime.HasValue 
                    ? session.StartTime.Value.ToString("dd MMM HH:mm") 
                    : "TBA";
                raceText += $"\n• {session.SessionName} ({session.SessionType}): {timeStr}";
            }
        }

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            raceText,
            cancellationToken: cancellationToken);
    }

    private async Task HandleScheduleCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);

        if (!round.HasValue)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Please provide a valid round number.\nExample: /schedule 5\nOr: /schedule 5 2023",
                cancellationToken: cancellationToken);
            return;
        }

        var schedule = await _sessionService.GetRaceScheduleAsync(round.Value, year);

        if (schedule == null || schedule.Sessions.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                $"❌ No schedule found for round {round}.",
                cancellationToken: cancellationToken);
            return;
        }

        var scheduleText = $"📅 {schedule.RaceName} - Round {round}\n\n";
        
        foreach (var session in schedule.Sessions.OrderBy(s => s.StartTime))
        {
            var timeStr = session.StartTime.HasValue 
                ? session.StartTime.Value.ToString("dd MMM HH:mm") 
                : "TBA";
            scheduleText += $"{session.SessionName} ({session.SessionType}): {timeStr}\n";
        }

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            scheduleText,
            cancellationToken: cancellationToken);
    }

    private async Task HandleResultsCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);

        List<RaceResult> results;

        if (round.HasValue)
        {
            results = await _raceResultsService.GetRaceResultsByRoundAsync(round.Value, year);
        }
        else
        {
            results = await _raceResultsService.GetLastRaceResultsAsync(year);
        }

        if (results.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No race results available.",
                cancellationToken: cancellationToken);
            return;
        }

        var resultsText = "🏁 Race Results\n\n";
        foreach (var result in results.Take(10))
        {
            resultsText += $"P{result.Position}. {result.DriverName} ({result.TeamName}) - {result.Points} pts\n";
        }

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            resultsText,
            cancellationToken: cancellationToken);
    }

    private async Task HandleUnknownCommandAsync(Message message, CancellationToken cancellationToken)
    {
        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            "❓ Unknown command. Use /help to see available commands.",
            cancellationToken: cancellationToken);
    }

    private async Task<(int? year, int? round)> ParseYearRoundArgsAsync(string[] arguments, long userId)
    {
        int? year = null;
        int? round = null;

        if (arguments.Length == 0)
        {
            var userState = await _userStateService.GetUserStateAsync(userId);
            if (userState.IsHistoryMode && userState.SelectedYear.HasValue)
            {
                year = userState.SelectedYear.Value;
            }
            return (year, round);
        }

        if (arguments.Length == 1)
        {
            if (int.TryParse(arguments[0], out var value))
            {
                if (value >= 1950 && value <= DateTime.UtcNow.Year + 1)
                {
                    year = value;
                }
                else if (value >= 1 && value <= 24)
                {
                    round = value;
                    var userState = await _userStateService.GetUserStateAsync(userId);
                    if (userState.IsHistoryMode && userState.SelectedYear.HasValue)
                    {
                        year = userState.SelectedYear.Value;
                    }
                }
            }
        }
        else if (arguments.Length >= 2)
        {
            if (int.TryParse(arguments[0], out var first) && int.TryParse(arguments[1], out var second))
            {
                if (first >= 1950 && first <= DateTime.UtcNow.Year + 1)
                {
                    year = first;
                    round = second;
                }
                else if (second >= 1950 && second <= DateTime.UtcNow.Year + 1)
                {
                    round = first;
                    year = second;
                }
            }
        }

        return (year, round);
    }

    private async Task HandleHistoryCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        if (arguments.Length == 0 || !int.TryParse(arguments[0], out var year))
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Please provide a valid year.\nExample: /history 2023",
                cancellationToken: cancellationToken);
            return;
        }

        if (!OpenF1CalendarService.IsValidYear(year))
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                $"❌ Invalid year {year}. Valid range: 1950-{DateTime.UtcNow.Year + 1}",
                cancellationToken: cancellationToken);
            return;
        }

        await _userStateService.SetHistoryModeAsync(message.From!.Id, year);
        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            $"✅ Switched to History Mode | Year: {year}",
            cancellationToken: cancellationToken);
    }

    private async Task HandleCurrentCommandAsync(Message message, CancellationToken cancellationToken)
    {
        await _userStateService.SetCurrentModeAsync(message.From!.Id);
        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            $"✅ Switched to Current Mode | Year: {DateTime.UtcNow.Year}",
            cancellationToken: cancellationToken);
    }

    private async Task HandleModeCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var userState = await _userStateService.GetUserStateAsync(message.From!.Id);
        var modeText = userState.IsHistoryMode
            ? $"History Mode | Year: {userState.SelectedYear}"
            : $"Current Mode | Year: {DateTime.UtcNow.Year}";

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            $"📊 {modeText}",
            cancellationToken: cancellationToken);
    }

    private async Task HandleCalendarCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, _) = await ParseYearRoundArgsAsync(arguments, message.From!.Id);

        var races = await _calendarService.GetRacesAsync(year);

        if (races.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                $"❌ No race calendar available for year {year ?? DateTime.UtcNow.Year}.",
                cancellationToken: cancellationToken);
            return;
        }

        var calendarText = $"📅 Race Calendar {(year.HasValue ? year.ToString() : DateTime.UtcNow.Year.ToString())}\n\n";
        foreach (var race in races)
        {
            calendarText += $"R{race.RoundNumber}. {race.Name}\n";
            calendarText += $"   {race.CircuitName}, {race.Country}\n";
            calendarText += $"   {race.Date:dd MMM yyyy}\n\n";
        }

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            calendarText,
            cancellationToken: cancellationToken);
    }
}
