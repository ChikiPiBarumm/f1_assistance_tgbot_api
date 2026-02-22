using F1_Bot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot.Handlers;

public class StandingsHandler : IStandingsHandler
{
    private readonly MessageSender _messageSender;
    private readonly IStandingsService _standingsService;
    private readonly IUserStateService _userStateService;
    private readonly IArgumentParser _argumentParser;
    private readonly IRaceDetailsService _raceDetailsService;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<StandingsHandler> _logger;

    public StandingsHandler(
        MessageSender messageSender,
        IStandingsService standingsService,
        IUserStateService userStateService,
        IArgumentParser argumentParser,
        IRaceDetailsService raceDetailsService,
        ITelegramBotClient botClient,
        ILogger<StandingsHandler> logger)
    {
        _messageSender = messageSender;
        _standingsService = standingsService;
        _userStateService = userStateService;
        _argumentParser = argumentParser;
        _raceDetailsService = raceDetailsService;
        _botClient = botClient;
        _logger = logger;
    }

    public async Task ShowStandingsChoiceAsync(Message message, CancellationToken cancellationToken)
    {
        var effectiveYear = await _userStateService.GetEffectiveYearAsync(message.From!.Id);

        var driverStandings = await _standingsService.GetDriverStandingsAsync(effectiveYear, null);
        if (driverStandings.Count == 0)
        {
            var noStandingsText = effectiveYear == DateTime.UtcNow.Year
                ? $"📊 Standings are not available yet for the {effectiveYear} season. Championship data will appear after the first race has been completed."
                : $"📊 No standings data is available for {effectiveYear}.";

            var backKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Go back", "standings|choice_back") }
            });

            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                noStandingsText,
                backKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏎 Driver Standings", $"standings|drivers|{effectiveYear}|"),
                InlineKeyboardButton.WithCallbackData("🏢 Team Standings", $"standings|teams|{effectiveYear}|")
            },
            new[] { InlineKeyboardButton.WithCallbackData("⬅️ Go back", "standings|choice_back") }
        });

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            "Which standings would you like to see?",
            keyboard,
            cancellationToken: cancellationToken);
    }

    public async Task HandleDriverStandingsAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await _argumentParser.ParseYearRoundAsync(arguments, message.From!.Id, cancellationToken);
        int? meetingKey = arguments.Length > 2 && int.TryParse(arguments[2], out var mk) ? mk : null;
        var standings = await _standingsService.GetDriverStandingsAsync(year, round, meetingKey);

        if (standings.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No driver standings available.",
                cancellationToken: cancellationToken);
            return;
        }

        var effectiveYear = year ?? DateTime.UtcNow.Year;
        var headingSuffix = await GetStandingsHeadingSuffixAsync(effectiveYear, round, meetingKey, cancellationToken);

        var standingsText = $"🏆 Driver Championship Standings {effectiveYear}{headingSuffix}\n\n";
        foreach (var standing in standings.Take(10))
        {
            var driverLabel = string.IsNullOrWhiteSpace(standing.DriverName) ? $"#{standing.DriverNumber}" : standing.DriverName;
            standingsText += $"{standing.Position}. {driverLabel} - {standing.Points} pts\n";
        }

        var inlineKeyboard = BuildStandingsBackKeyboard();

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                standingsText,
                inlineKeyboard,
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

    public async Task HandleTeamStandingsAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await _argumentParser.ParseYearRoundAsync(arguments, message.From!.Id, cancellationToken);
        int? meetingKey = arguments.Length > 2 && int.TryParse(arguments[2], out var mk) ? mk : null;
        var standings = await _standingsService.GetTeamStandingsAsync(year, round, meetingKey);

        if (standings.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No team standings available.",
                cancellationToken: cancellationToken);
            return;
        }

        var effectiveYear = year ?? DateTime.UtcNow.Year;
        var headingSuffix = await GetStandingsHeadingSuffixAsync(effectiveYear, round, meetingKey, cancellationToken);

        var standingsText = $"🏆 Constructor Championship Standings {effectiveYear}{headingSuffix}\n\n";
        foreach (var standing in standings.Take(10))
        {
            standingsText += $"{standing.Position}. {standing.TeamName} - {standing.Points} pts\n";
        }

        var inlineKeyboard = BuildStandingsBackKeyboard();

        try
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                standingsText,
                inlineKeyboard,
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

    public async Task HandleStandingsCallbackAsync(Message message, string action, string? yearValue, string? roundValue, string? meetingKeyValue, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "drivers":
            {
                var args = new[] { yearValue ?? "", roundValue ?? "", meetingKeyValue ?? "" };
                await HandleDriverStandingsAsync(message, args, cancellationToken);
                break;
            }
            case "teams":
            {
                var args = new[] { yearValue ?? "", roundValue ?? "", meetingKeyValue ?? "" };
                await HandleTeamStandingsAsync(message, args, cancellationToken);
                break;
            }
            case "byround":
                await _messageSender.SendMessageAsync(
                    message.Chat.Id,
                    "Work in progress.",
                    cancellationToken: cancellationToken);
                break;
            case "back":
            case "choice_back":
                try
                {
                    await _botClient.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete standings message");
                }
                break;
        }
    }

    private async Task<string> GetStandingsHeadingSuffixAsync(int year, int? round, int? meetingKey, CancellationToken cancellationToken)
    {
        if (!meetingKey.HasValue || !round.HasValue)
            return string.Empty;

        var race = await _raceDetailsService.GetRaceByMeetingKeyAsync(meetingKey.Value, round.Value, year);
        if (race == null)
            return string.Empty;

        return $" (Round {round.Value} - {race.Name})";
    }

    private static InlineKeyboardMarkup BuildStandingsBackKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("⬅️ Go back", "standings|back") }
        });
    }
}
