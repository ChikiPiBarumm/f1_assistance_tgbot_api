using System.Linq;
using F1_Bot.Application.Interfaces;
using F1_Bot.Presentation.Bot.Handlers;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using ChatAction = Telegram.Bot.Types.Enums.ChatAction;

namespace F1_Bot.Presentation.Bot;

public class TelegramBotCommandRouter
{
    private readonly MessageSender _messageSender;
    private readonly IStartHandler _startHandler;
    private readonly ICalendarHandler _calendarHandler;
    private readonly IRaceDetailsHandler _raceDetailsHandler;
    private readonly IStandingsHandler _standingsHandler;
    private readonly IResultsHandler _resultsHandler;
    private readonly IModeHandler _modeHandler;
    private readonly ICalendarService _calendarService;
    private readonly ISessionService _sessionService;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<TelegramBotCommandRouter> _logger;

    public TelegramBotCommandRouter(
        MessageSender messageSender,
        IStartHandler startHandler,
        ICalendarHandler calendarHandler,
        IRaceDetailsHandler raceDetailsHandler,
        IStandingsHandler standingsHandler,
        IResultsHandler resultsHandler,
        IModeHandler modeHandler,
        ICalendarService calendarService,
        ISessionService sessionService,
        ITelegramBotClient botClient,
        ILogger<TelegramBotCommandRouter> logger)
    {
        _messageSender = messageSender;
        _startHandler = startHandler;
        _calendarHandler = calendarHandler;
        _raceDetailsHandler = raceDetailsHandler;
        _standingsHandler = standingsHandler;
        _resultsHandler = resultsHandler;
        _modeHandler = modeHandler;
        _calendarService = calendarService;
        _sessionService = sessionService;
        _botClient = botClient;
        _logger = logger;
    }

    private void SendTypingIndicator(ChatId chatId, CancellationToken cancellationToken)
    {
        _ = SendTypingInternalAsync(chatId, cancellationToken);
    }

    private async Task SendTypingInternalAsync(ChatId chatId, CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.SendChatAction(chatId, ChatAction.Typing, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "SendChatAction Typing failed");
        }
    }

    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken = default)
    {
        if (update.CallbackQuery is { } callbackQuery)
        {
            await HandleCallbackQueryAsync(callbackQuery, cancellationToken);
            return;
        }

        if (update.Message is not { } message || message.Text is not { } messageText)
        {
            return;
        }

        _logger.LogDebug("Received message: {Text} from user {UserId}", messageText, message.From?.Id);

        try
        {
            var parts = messageText.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            var command = parts[0].ToLowerInvariant();
            var arguments = parts.Skip(1).ToArray();

            using var commandCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, commandCts.Token);

            switch (command)
            {
                case "/start":
                    await _startHandler.HandleStartAsync(message, linkedCts.Token);
                    break;

                case "/history":
                case "/history_mode":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _modeHandler.HandleHistoryAsync(message, arguments, linkedCts.Token);
                    break;

                case "/current":
                case "/current_mode":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _modeHandler.HandleCurrentAsync(message, linkedCts.Token);
                    break;

                case "/mode":
                case "/status":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _modeHandler.HandleModeAsync(message, linkedCts.Token);
                    break;

                case "/next_race":
                case "/nextrace":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _raceDetailsHandler.HandleNextRaceAsync(message, arguments, linkedCts.Token);
                    break;

                case "/driver_standings":
                case "/driverstandings":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _standingsHandler.HandleDriverStandingsAsync(message, arguments, linkedCts.Token);
                    break;
                case "/standings":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    if (arguments.Length == 0)
                    {
                        await _standingsHandler.ShowStandingsChoiceAsync(message, linkedCts.Token);
                    }
                    else
                    {
                        await _standingsHandler.HandleDriverStandingsAsync(message, arguments, linkedCts.Token);
                    }
                    break;

                case "/team_standings":
                case "/teamstandings":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _standingsHandler.HandleTeamStandingsAsync(message, arguments, linkedCts.Token);
                    break;

                case "/last_race":
                case "/lastrace":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _raceDetailsHandler.HandleLastRaceInfoAsync(message, arguments, linkedCts.Token);
                    break;

                case "/results":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _raceDetailsHandler.HandleLastRaceInfoAsync(message, arguments, linkedCts.Token);
                    break;

                case "/race":
                case "/race_info":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _raceDetailsHandler.HandleRaceDetailsAsync(message, arguments, linkedCts.Token);
                    break;

                case "/schedule":
                case "/sessions":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await HandleScheduleCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/calendar":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _calendarHandler.HandleCalendarCommandAsync(message, arguments, linkedCts.Token);
                    break;

                case "/help":
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _startHandler.HandleHelpAsync(message, linkedCts.Token);
                    break;

                default:
                    SendTypingIndicator(message.Chat.Id, linkedCts.Token);
                    await _startHandler.HandleUnknownAsync(message, linkedCts.Token);
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
                // Ignore
            }
        }
    }

    private async Task HandleCallbackQueryAsync(Telegram.Bot.Types.CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(callbackQuery.Data) || callbackQuery.Message is null)
        {
            return;
        }

        var dataParts = callbackQuery.Data.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (dataParts.Length == 0)
        {
            return;
        }

        var feature = dataParts[0].ToLowerInvariant();
        var action = dataParts.Length > 1 ? dataParts[1].ToLowerInvariant() : string.Empty;
        var p1 = dataParts.Length > 2 ? dataParts[2] : null;
        var p2 = dataParts.Length > 3 ? dataParts[3] : null;
        var p3 = dataParts.Length > 4 ? dataParts[4] : null;

        try
        {
            await _botClient.AnswerCallbackQuery(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch
        {
            // Non-critical
        }

        var message = callbackQuery.Message;

        SendTypingIndicator(message.Chat.Id, cancellationToken);

        switch (feature)
        {
            case "nav":
                await HandleNavigationCallbackAsync(message, action, cancellationToken);
                break;

            case "calendar":
                await _calendarHandler.HandleCalendarCallbackAsync(message, action, p1, p2, p3, cancellationToken);
                break;

            case "race":
                await _raceDetailsHandler.HandleRaceCallbackAsync(message, action, p1, p2, p3, cancellationToken);
                break;

            case "results":
                await _resultsHandler.HandleResultsCallbackAsync(message, action, p1, p2, cancellationToken);
                break;

            case "standings":
                await _standingsHandler.HandleStandingsCallbackAsync(message, action, p1, p2, p3, cancellationToken);
                break;

            case "mode":
                await _modeHandler.HandleModeCallbackAsync(message, action, p1, cancellationToken);
                break;
        }
    }

    private async Task HandleNavigationCallbackAsync(Message message, string action, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "calendar":
                await _calendarHandler.HandleCalendarCommandAsync(message, Array.Empty<string>(), cancellationToken);
                break;
            case "next":
                await _raceDetailsHandler.HandleNextRaceAsync(message, Array.Empty<string>(), cancellationToken);
                break;
            case "standings":
                await _standingsHandler.ShowStandingsChoiceAsync(message, cancellationToken);
                break;
            case "results":
                await _raceDetailsHandler.HandleLastRaceInfoAsync(message, Array.Empty<string>(), cancellationToken);
                break;
            case "mode":
                await _modeHandler.HandleModeAsync(message, cancellationToken);
                break;
            case "help":
                await _startHandler.HandleHelpAsync(message, cancellationToken);
                break;
            case "main":
                await _startHandler.HandleStartAsync(message, cancellationToken);
                break;
        }
    }

    private async Task HandleScheduleCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        F1_Bot.Domain.Models.RaceSchedule? schedule = null;
        if (arguments.Length > 0 && int.TryParse(arguments[0], out var round) && round >= 1)
        {
            schedule = await _sessionService.GetRaceScheduleAsync(round);
        }
        else
        {
            var nextRace = await _calendarService.GetNextRaceAsync();
            if (nextRace != null)
                schedule = await _sessionService.GetRaceScheduleByMeetingKeyAsync(nextRace.Id);
        }

        if (schedule == null || schedule.Sessions.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No session schedule available. Try /next_race or /schedule followed by a round number (e.g. /schedule 5).",
                cancellationToken: cancellationToken);
            return;
        }

        var lines = new List<string> { $"📅 {schedule.RaceName}", "" };
        foreach (var s in schedule.Sessions)
        {
            var timeStr = s.StartTime.HasValue ? s.StartTime.Value.ToString("ddd dd MMM HH:mm UTC") : "TBC";
            lines.Add($"• {s.SessionName} ({s.SessionType}): {timeStr}");
        }

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            string.Join("\n", lines),
            cancellationToken: cancellationToken);
    }
}
