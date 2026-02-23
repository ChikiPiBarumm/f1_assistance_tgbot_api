using System.Linq;
using F1_Bot.Application.Interfaces;
using F1_Bot.Domain.Models;
using F1_Bot.Presentation.Bot;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot.Handlers;

public class ResultsHandler : IResultsHandler
{
    private readonly MessageSender _messageSender;
    private readonly IRaceResultsService _raceResultsService;
    private readonly IRaceDetailsService _raceDetailsService;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<ResultsHandler> _logger;

    public ResultsHandler(
        MessageSender messageSender,
        IRaceResultsService raceResultsService,
        IRaceDetailsService raceDetailsService,
        ITelegramBotClient botClient,
        ILogger<ResultsHandler> logger)
    {
        _messageSender = messageSender;
        _raceResultsService = raceResultsService;
        _raceDetailsService = raceDetailsService;
        _botClient = botClient;
        _logger = logger;
    }

    public async Task HandleResultsByMeetingKeyAsync(Message message, int meetingKey, int year, int round, CancellationToken cancellationToken)
    {
        var raceDetails = await _raceDetailsService.GetRaceByMeetingKeyAsync(meetingKey, round, year);

        if (IsTestingEvent(raceDetails))
        {
            var testingMessage = "📋 In the current version, viewing testing results is not available. This will be added in a future update.";
            var testingKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("⬅️ Go back", "results|back_to_race") }
            });
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                testingMessage,
                testingKeyboard,
                cancellationToken: cancellationToken);
            return;
        }

        var results = await _raceResultsService.GetRaceResultsByMeetingKeyAsync(meetingKey);

        if (results.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No race results available.",
                cancellationToken: cancellationToken);
            return;
        }

        var resultsText = BuildResultsText(results, raceDetails, year, null);
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🏎 Driver Standings", $"standings|drivers|{year}|{round}|{meetingKey}"),
                InlineKeyboardButton.WithCallbackData("🏢 Team Standings", $"standings|teams|{year}|{round}|{meetingKey}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Back to Race Info", "results|back_to_race"),
                InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main")
            }
        });

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            resultsText,
            keyboard,
            cancellationToken: cancellationToken);
    }

    private static string BuildResultsText(List<RaceResult> results, RaceDetails? raceDetails, int year, string? raceName = null)
    {
        var heading = raceName != null
            ? $"🏁 {raceName} {year}\n\n"
            : raceDetails != null
                ? $"🏁 {raceDetails.Name} {year}\n\n"
                : $"🏁 Race Results {year}\n\n";
        var text = heading;
        foreach (var result in results.Take(10))
        {
            var driverLabel = string.IsNullOrWhiteSpace(result.DriverName) ? $"#{result.DriverNumber}" : result.DriverName;
            text += $"P{result.Position}. {driverLabel} — {result.Points} pts\n";
        }
        return text;
    }

    private static bool IsTestingEvent(RaceDetails? race)
    {
        if (race?.Name == null) return false;
        var name = race.Name;
        return name.Contains("Test", StringComparison.OrdinalIgnoreCase) || name.Contains("Testing", StringComparison.OrdinalIgnoreCase);
    }

    public async Task HandleResultsCallbackAsync(Message message, string action, string? yearValue, string? roundValue, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "back_to_race":
                await TelegramBotHelpers.TryDeleteMessageAsync(_botClient, message.Chat.Id, message.MessageId, _logger, "results message", cancellationToken);
                break;
        }
    }
}
