using F1_Bot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot.Handlers;

public class StandingsHandler : IStandingsHandler
{
    private readonly MessageSender _messageSender;
    private readonly IStandingsService _standingsService;
    private readonly IUserStateService _userStateService;
    private readonly IArgumentParser _argumentParser;
    private readonly ILogger<StandingsHandler> _logger;

    public StandingsHandler(
        MessageSender messageSender,
        IStandingsService standingsService,
        IUserStateService userStateService,
        IArgumentParser argumentParser,
        ILogger<StandingsHandler> logger)
    {
        _messageSender = messageSender;
        _standingsService = standingsService;
        _userStateService = userStateService;
        _argumentParser = argumentParser;
        _logger = logger;
    }

    public async Task ShowStandingsChoiceAsync(Message message, CancellationToken cancellationToken)
    {
        var effectiveYear = await _userStateService.GetEffectiveYearAsync(message.From!.Id);

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏎 Driver Standings", $"standings|drivers|{effectiveYear}|"),
                InlineKeyboardButton.WithCallbackData("🏢 Team Standings", $"standings|teams|{effectiveYear}|")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📅 By Round", $"standings|byround|{effectiveYear}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"⚙️ Season {effectiveYear}", "nav|mode"),
                InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main")
            }
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
        var standings = await _standingsService.GetDriverStandingsAsync(year, round);

        if (standings.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No driver standings available.",
                cancellationToken: cancellationToken);
            return;
        }

        var effectiveYear = year ?? DateTime.UtcNow.Year;

        var standingsText = $"🏆 Driver Championship Standings {effectiveYear}\n\n";
        foreach (var standing in standings.Take(10))
        {
            standingsText += $"{standing.Position}. {standing.DriverName} ({standing.TeamName}) - {standing.Points} pts\n";
        }

        var inlineKeyboard = BuildStandingsKeyboard(effectiveYear, round);

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
        var standings = await _standingsService.GetTeamStandingsAsync(year, round);

        if (standings.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No team standings available.",
                cancellationToken: cancellationToken);
            return;
        }

        var effectiveYear = year ?? DateTime.UtcNow.Year;

        var standingsText = $"🏆 Constructor Championship Standings {effectiveYear}\n\n";
        foreach (var standing in standings.Take(10))
        {
            standingsText += $"{standing.Position}. {standing.TeamName} - {standing.Points} pts\n";
        }

        var inlineKeyboard = BuildStandingsKeyboard(effectiveYear, round);

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

    public async Task HandleStandingsCallbackAsync(Message message, string action, string? yearValue, string? roundValue, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "drivers":
            {
                var args = new[] { yearValue ?? "", roundValue ?? "" };
                await HandleDriverStandingsAsync(message, args, cancellationToken);
                break;
            }
            case "teams":
            {
                var args = new[] { yearValue ?? "", roundValue ?? "" };
                await HandleTeamStandingsAsync(message, args, cancellationToken);
                break;
            }
            case "byround":
                await _messageSender.SendMessageAsync(
                    message.Chat.Id,
                    "Work in progress.",
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private static InlineKeyboardMarkup BuildStandingsKeyboard(int effectiveYear, int? round)
    {
        var roundStr = round?.ToString() ?? string.Empty;
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏎 Driver Standings", $"standings|drivers|{effectiveYear}|{roundStr}"),
                InlineKeyboardButton.WithCallbackData("🏢 Team Standings", $"standings|teams|{effectiveYear}|{roundStr}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📅 By Round", $"standings|byround|{effectiveYear}"),
                InlineKeyboardButton.WithCallbackData($"⚙️ Season {effectiveYear}", "nav|mode")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main")
            }
        });
    }
}
