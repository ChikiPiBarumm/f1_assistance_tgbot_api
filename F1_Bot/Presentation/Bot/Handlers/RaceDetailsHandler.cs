using System.Linq;
using F1_Bot.Domain.Models;
using F1_Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot.Handlers;

public class RaceDetailsHandler : IRaceDetailsHandler
{
    private readonly MessageSender _messageSender;
    private readonly ICalendarService _calendarService;
    private readonly IRaceDetailsService _raceDetailsService;
    private readonly IRaceResultsService _raceResultsService;
    private readonly IArgumentParser _argumentParser;
    private readonly ITelegramBotClient _botClient;
    private readonly IResultsHandler _resultsHandler;

    public RaceDetailsHandler(
        MessageSender messageSender,
        ICalendarService calendarService,
        IRaceDetailsService raceDetailsService,
        IRaceResultsService raceResultsService,
        IArgumentParser argumentParser,
        ITelegramBotClient botClient,
        IResultsHandler resultsHandler)
    {
        _messageSender = messageSender;
        _calendarService = calendarService;
        _raceDetailsService = raceDetailsService;
        _raceResultsService = raceResultsService;
        _argumentParser = argumentParser;
        _botClient = botClient;
        _resultsHandler = resultsHandler;
    }

    public async Task HandleNextRaceAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, _) = await _argumentParser.ParseYearRoundAsync(arguments, message.From!.Id, cancellationToken);
        var effectiveYear = year ?? DateTime.UtcNow.Year;
        var nextRace = await _calendarService.GetNextRaceAsync(effectiveYear);

        if (nextRace == null)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No upcoming race found.",
                cancellationToken: cancellationToken);
            return;
        }

        await HandleRaceDetailsByMeetingKeyAsync(message, nextRace.Id, nextRace.RoundNumber, effectiveYear, cancellationToken, fromNextRace: true);
    }

    public async Task HandleLastRaceInfoAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var meetingInfo = await _raceResultsService.GetLastRaceMeetingInfoAsync();

        if (meetingInfo == null)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ No race information available.",
                cancellationToken: cancellationToken);
            return;
        }

        await HandleRaceDetailsByMeetingKeyAsync(
            message,
            meetingInfo.Value.MeetingKey,
            meetingInfo.Value.Round,
            meetingInfo.Value.Year,
            cancellationToken,
            fromNextRace: false);
    }

    public async Task HandleRaceDetailsAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, round) = await _argumentParser.ParseYearRoundAsync(arguments, message.From!.Id, cancellationToken);

        if (!round.HasValue)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Please provide a valid round number.\nExample: /race 5\nOr: /race 5 2023",
                cancellationToken: cancellationToken);
            return;
        }

        var effectiveYear = year ?? DateTime.UtcNow.Year;

        var race = await _raceDetailsService.GetRaceByRoundAsync(round.Value, year);

        if (race == null)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                $"❌ Race not found for round {round}.",
                cancellationToken: cancellationToken);
            return;
        }

        var raceText = BuildRaceText(race);
        var races = await _calendarService.GetRacesAsync(effectiveYear);
        var keyboard = BuildRaceDetailsKeyboard(races.OrderBy(r => r.RoundNumber).ToList(), round.Value, effectiveYear, race.Status);

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            raceText,
            keyboard,
            cancellationToken: cancellationToken);
    }

    public async Task HandleRaceDetailsByMeetingKeyAsync(Message message, int meetingKey, int round, int year, CancellationToken cancellationToken, bool fromNextRace = false)
    {
        var content = await GetRaceContentByMeetingKeyAsync(meetingKey, round, year, cancellationToken);
        if (content == null)
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Race not found for meeting.",
                cancellationToken: cancellationToken);
            return;
        }

        var keyboard = fromNextRace
            ? new InlineKeyboardMarkup(new[] { new[] { InlineKeyboardButton.WithCallbackData("⬅️ Back to Menu", "race|back_to_menu") } })
            : content.Value.keyboard;

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            content.Value.raceText,
            keyboard,
            cancellationToken: cancellationToken);
    }

    public async Task EditRaceDetailsByMeetingKeyAsync(Message message, int meetingKey, int round, int year, CancellationToken cancellationToken)
    {
        var content = await GetRaceContentByMeetingKeyAsync(meetingKey, round, year, cancellationToken);
        if (content == null)
        {
            await _botClient.EditMessageText(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: "❌ Race not found for meeting.",
                cancellationToken: cancellationToken);
            return;
        }

        await _botClient.EditMessageText(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: content.Value.raceText,
            replyMarkup: content.Value.keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task<(string raceText, InlineKeyboardMarkup keyboard)?> GetRaceContentByMeetingKeyAsync(int meetingKey, int round, int year, CancellationToken cancellationToken)
    {
        var race = await _raceDetailsService.GetRaceByMeetingKeyAsync(meetingKey, round, year);
        if (race == null)
            return null;

        var raceText = BuildRaceText(race);
        var races = await _calendarService.GetRacesAsync(year);
        var keyboard = BuildRaceDetailsKeyboard(races.OrderBy(r => r.RoundNumber).ToList(), round, year, race.Status);
        return (raceText, keyboard);
    }

    public async Task HandleRaceCallbackAsync(Message message, string action, string? yearValue, string? roundValue, string? meetingKeyValue, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "back_to_menu":
                try
                {
                    await _botClient.DeleteMessage(message.Chat.Id, message.MessageId, cancellationToken);
                }
                catch
                {
                    // Non-critical
                }
                break;
            case "results" when int.TryParse(meetingKeyValue, out var meetingKey):
            {
                var year = int.TryParse(yearValue, out var y) ? y : DateTime.UtcNow.Year;
                var round = int.TryParse(roundValue, out var r) ? r : 1;
                await _resultsHandler.HandleResultsByMeetingKeyAsync(message, meetingKey, year, round, cancellationToken);
                break;
            }
        }
    }

    private static string BuildRaceText(RaceDetails race)
    {
        return $"🏁 {race.Name}\n\n" +
               $"📍 {race.CircuitName}\n" +
               $"🌍 {race.City}, {race.Country}\n" +
               $"📅 {race.Date:dd MMMM yyyy}\n" +
               $"🔢 Round {race.RoundNumber}\n" +
               $"📊 Status: {race.Status}";
    }

    private static InlineKeyboardMarkup BuildRaceDetailsKeyboard(List<Race> races, int round, int effectiveYear, string raceStatus)
    {
        var orderedRaces = races;
        var currentRaceIndex = orderedRaces.FindIndex(r => r.RoundNumber == round);

        var navButtons = new List<InlineKeyboardButton>();
        if (currentRaceIndex > 0)
        {
            var prevRace = orderedRaces[currentRaceIndex - 1];
            navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Previous Race", $"calendar|race|{effectiveYear}|{prevRace.RoundNumber}|{prevRace.Id}"));
        }
        if (currentRaceIndex < orderedRaces.Count - 1 && currentRaceIndex >= 0)
        {
            var nextRace = orderedRaces[currentRaceIndex + 1];
            navButtons.Add(InlineKeyboardButton.WithCallbackData("Next Race ➡️", $"calendar|race|{effectiveYear}|{nextRace.RoundNumber}|{nextRace.Id}"));
        }

        var keyboardRows = new List<InlineKeyboardButton[]>();
        if (navButtons.Count > 0)
        {
            keyboardRows.Add(navButtons.ToArray());
        }
        if (string.Equals(raceStatus, "Completed", StringComparison.OrdinalIgnoreCase))
        {
            var currentRace = orderedRaces.FirstOrDefault(r => r.RoundNumber == round);
            var meetingKey = currentRace?.Id ?? 0;
            keyboardRows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🏁 Results", $"race|results|{effectiveYear}|{round}|{meetingKey}")
            });
        }
        keyboardRows.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("⬅️ Back to Calendar", $"calendar|show|{effectiveYear}"),
            InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main")
        });

        return new InlineKeyboardMarkup(keyboardRows);
    }
}
