using System.Linq;
using F1_Bot.Domain.Models;
using F1_Bot.Services;
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

    public async Task HandleLastRaceResultsAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (results, meetingKey) = await _raceResultsService.GetLastRaceResultsWithMeetingKeyAsync();

        if (results.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No race results available.",
                cancellationToken: cancellationToken);
            return;
        }

        var raceDetails = await _raceDetailsService.GetRaceByMeetingKeyAsync(meetingKey, round: 0, year: null);
        var year = raceDetails?.Date.Year ?? DateTime.UtcNow.Year;
        var resultsText = BuildResultsText(results, raceDetails, year);
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Back to Menu", "results|back_to_race")
            }
        });

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            resultsText,
            keyboard,
            cancellationToken: cancellationToken);
    }

    public async Task HandleResultsByMeetingKeyAsync(Message message, int meetingKey, int year, int round, CancellationToken cancellationToken)
    {
        var results = await _raceResultsService.GetRaceResultsByMeetingKeyAsync(meetingKey);

        if (results.Count == 0)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No race results available.",
                cancellationToken: cancellationToken);
            return;
        }

        var raceDetails = await _raceDetailsService.GetRaceByMeetingKeyAsync(meetingKey, round, year);
        var resultsText = BuildResultsText(results, raceDetails, year);
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📊 Driver Standings", $"standings|drivers|{year}|"),
                InlineKeyboardButton.WithCallbackData("🏢 Team Standings", $"standings|teams|{year}|")
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

    private static string BuildResultsText(List<RaceResult> results, RaceDetails? raceDetails, int year)
    {
        var heading = raceDetails != null
            ? $"🏁 {raceDetails.Name} {year}\n\n"
            : $"🏁 Race Results {year}\n\n";
        var text = heading;
        foreach (var result in results.Take(10))
        {
            text += $"P{result.Position}. #{result.DriverNumber} — {result.Points} pts\n";
        }
        return text;
    }

    public async Task HandleResultsCallbackAsync(Message message, string action, string? yearValue, string? roundValue, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "back_to_race":
                try
                {
                    await _botClient.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not delete results message");
                }
                break;
        }
    }
}
