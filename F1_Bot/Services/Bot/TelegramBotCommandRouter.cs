using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using F1_Bot.Services;

namespace F1_Bot.Services.Bot;

public class TelegramBotCommandRouter
{
    private readonly TelegramBotMessageSender _messageSender;
    private readonly ICalendarService _calendarService;
    private readonly IStandingsService _standingsService;
    private readonly IRaceResultsService _raceResultsService;
    private readonly ILogger<TelegramBotCommandRouter> _logger;

    public TelegramBotCommandRouter(
        TelegramBotMessageSender messageSender,
        ICalendarService calendarService,
        IStandingsService standingsService,
        IRaceResultsService raceResultsService,
        ILogger<TelegramBotCommandRouter> logger)
    {
        _messageSender = messageSender;
        _calendarService = calendarService;
        _standingsService = standingsService;
        _raceResultsService = raceResultsService;
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
            var command = messageText.Split(' ')[0].ToLowerInvariant();

            using var commandCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, commandCts.Token);
            
            switch (command)
            {
                case "/start":
                    await HandleStartCommandAsync(message, linkedCts.Token);
                    break;

                case "/next_race":
                case "/nextrace":
                    await HandleNextRaceCommandAsync(message, linkedCts.Token);
                    break;

                case "/driver_standings":
                case "/driverstandings":
                case "/standings":
                    await HandleDriverStandingsCommandAsync(message, linkedCts.Token);
                    break;

                case "/team_standings":
                case "/teamstandings":
                    await HandleTeamStandingsCommandAsync(message, linkedCts.Token);
                    break;

                case "/last_race":
                case "/lastrace":
                case "/results":
                    await HandleLastRaceResultsCommandAsync(message, linkedCts.Token);
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

I can help you with Formula 1 information:

/next_race - Get the next upcoming race
/standings - Driver championship standings
/team_standings - Constructor championship standings
/last_race - Results from the last race
/help - Show this help message

Use any command to get started!";

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                welcomeText,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            throw;
        }
    }

    private async Task HandleNextRaceCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var nextRace = await _calendarService.GetNextRaceAsync();

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

    private async Task HandleDriverStandingsCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var standings = await _standingsService.GetDriverStandingsAsync();

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

    private async Task HandleTeamStandingsCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var standings = await _standingsService.GetTeamStandingsAsync();

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

    private async Task HandleLastRaceResultsCommandAsync(Message message, CancellationToken cancellationToken)
    {
        var results = await _raceResultsService.GetLastRaceResultsAsync();

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

/start - Welcome message
/next_race - Get the next upcoming race
/standings - Driver championship standings
/team_standings - Constructor championship standings
/last_race - Results from the last race
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

    private async Task HandleUnknownCommandAsync(Message message, CancellationToken cancellationToken)
    {
        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            "❓ Unknown command. Use /help to see available commands.",
            cancellationToken: cancellationToken);
    }
}
