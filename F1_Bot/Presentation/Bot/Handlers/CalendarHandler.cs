using System.Linq;
using F1_Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot.Handlers;

public class CalendarHandler : ICalendarHandler
{
    private const int PageSize = 7;

    private readonly MessageSender _messageSender;
    private readonly ICalendarService _calendarService;
    private readonly IArgumentParser _argumentParser;
    private readonly ITelegramBotClient _botClient;
    private readonly IRaceDetailsHandler _raceDetailsHandler;

    public CalendarHandler(
        MessageSender messageSender,
        ICalendarService calendarService,
        IArgumentParser argumentParser,
        ITelegramBotClient botClient,
        IRaceDetailsHandler raceDetailsHandler)
    {
        _messageSender = messageSender;
        _calendarService = calendarService;
        _argumentParser = argumentParser;
        _botClient = botClient;
        _raceDetailsHandler = raceDetailsHandler;
    }

    public async Task HandleCalendarCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        var (year, _) = await _argumentParser.ParseYearRoundAsync(arguments, message.From!.Id, cancellationToken);
        var effectiveYear = year ?? DateTime.UtcNow.Year;
        await SendCalendarPageAsync(message.Chat.Id, effectiveYear, 1, cancellationToken);
    }

    public async Task HandleCalendarCallbackAsync(Message message, string action, string? yearValue, string? roundValue, string? meetingKeyValue, CancellationToken cancellationToken)
    {
        switch (action)
        {
            case "race" when int.TryParse(roundValue, out var round) && int.TryParse(meetingKeyValue, out var meetingKey):
            {
                var year = int.TryParse(yearValue, out var y) ? y : (int?)null;
                var effectiveYear = year ?? DateTime.UtcNow.Year;

                if (message.Text != null && (message.Text.Contains("Round") || message.Text.StartsWith("🏁")))
                {
                    await _raceDetailsHandler.EditRaceDetailsByMeetingKeyAsync(message, meetingKey, round, effectiveYear, cancellationToken);
                }
                else
                {
                    await _raceDetailsHandler.HandleRaceDetailsByMeetingKeyAsync(message, meetingKey, round, effectiveYear, cancellationToken);
                }
                break;
            }
            case "show" when int.TryParse(yearValue, out var showYear):
                await EditCalendarPageAsync(message, showYear, 1, cancellationToken);
                break;
            case "page" when int.TryParse(yearValue, out var year) && int.TryParse(roundValue, out var page):
                await EditCalendarPageAsync(message, year, page, cancellationToken);
                break;
        }
    }

    public async Task SendCalendarPageAsync(ChatId chatId, int year, int page, CancellationToken cancellationToken)
    {
        var content = await GetCalendarPageContentAsync(year, page, cancellationToken);
        if (content == null)
        {
            await _messageSender.SendMessageAsync(
                chatId,
                $"❌ No race calendar available for year {year}.",
                cancellationToken: cancellationToken);
            return;
        }

        await _messageSender.SendMessageAsync(
            chatId,
            content.Value.header,
            content.Value.keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task EditCalendarPageAsync(Message message, int year, int page, CancellationToken cancellationToken)
    {
        var content = await GetCalendarPageContentAsync(year, page, cancellationToken);
        if (content == null)
        {
            await _botClient.EditMessageText(
                chatId: message.Chat.Id,
                messageId: message.MessageId,
                text: $"❌ No race calendar available for year {year}.",
                cancellationToken: cancellationToken);
            return;
        }

        await _botClient.EditMessageText(
            chatId: message.Chat.Id,
            messageId: message.MessageId,
            text: content.Value.header,
            replyMarkup: content.Value.keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task<(string header, InlineKeyboardMarkup keyboard)?> GetCalendarPageContentAsync(int year, int page, CancellationToken cancellationToken)
    {
        var races = await _calendarService.GetRacesAsync(year);
        if (races.Count == 0)
            return null;

        var totalPages = Math.Max(1, (int)Math.Ceiling(races.Count / (double)PageSize));
        var currentPage = Math.Clamp(page, 1, totalPages);
        var skip = (currentPage - 1) * PageSize;

        var pageRaces = races
            .OrderBy(r => r.RoundNumber)
            .Skip(skip)
            .Take(PageSize)
            .ToList();

        var rows = new List<InlineKeyboardButton[]>();
        foreach (var race in pageRaces)
        {
            var label = $"R{race.RoundNumber}: {race.Name}";
            var data = $"calendar|race|{year}|{race.RoundNumber}|{race.Id}";
            rows.Add(new[] { InlineKeyboardButton.WithCallbackData(label, data) });
        }

        var navButtons = new List<InlineKeyboardButton>();
        if (currentPage > 1)
            navButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️ Prev", $"calendar|page|{year}|{currentPage - 1}"));
        if (currentPage < totalPages)
            navButtons.Add(InlineKeyboardButton.WithCallbackData("➡️ Next", $"calendar|page|{year}|{currentPage + 1}"));
        if (navButtons.Count > 0)
            rows.Add(navButtons.ToArray());

        rows.Add(new[] { InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main") });

        var keyboard = new InlineKeyboardMarkup(rows);
        var header = $"📅 Race Calendar {year}\nPage {currentPage}/{totalPages}\n\nSelect a race:";
        return (header, keyboard);
    }
}
