using F1_Bot.Domain.Models;
using F1_Bot.Services;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot.Handlers;

public class ModeHandler : IModeHandler
{
    private readonly MessageSender _messageSender;
    private readonly IUserStateService _userStateService;
    private readonly ITelegramBotClient _botClient;

    public ModeHandler(
        MessageSender messageSender,
        IUserStateService userStateService,
        ITelegramBotClient botClient)
    {
        _messageSender = messageSender;
        _userStateService = userStateService;
        _botClient = botClient;
    }

    public async Task HandleHistoryAsync(Message message, string[] arguments, CancellationToken cancellationToken)
    {
        if (arguments.Length == 0 || !int.TryParse(arguments[0], out var year))
        {
            await _messageSender.SendMessageAsync(
                message.Chat.Id,
                "❌ Please provide a valid year.\nExample: /history 2023",
                cancellationToken: cancellationToken);
            return;
        }

        if (!CalendarService.IsValidYear(year))
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

    public async Task HandleCurrentAsync(Message message, CancellationToken cancellationToken)
    {
        await _userStateService.SetCurrentModeAsync(message.From!.Id);
        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            $"✅ Switched to Current Mode | Year: {DateTime.UtcNow.Year}",
            cancellationToken: cancellationToken);
    }

    public async Task HandleModeAsync(Message message, CancellationToken cancellationToken)
    {
        var userState = await _userStateService.GetUserStateAsync(message.From!.Id);
        var (text, keyboard) = BuildModeStatusContent(userState);

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            text,
            keyboard,
            cancellationToken: cancellationToken);
    }

    public async Task HandleModeCallbackAsync(Message message, string action, string? yearValue, CancellationToken cancellationToken)
    {
        var userId = message.From?.Id;
        if (userId is null)
        {
            return;
        }

        switch (action)
        {
            case "to_current":
                await _userStateService.SetCurrentModeAsync(userId.Value);
                {
                    var state = await _userStateService.GetUserStateAsync(userId.Value);
                    var (text, keyboard) = BuildModeStatusContent(state);
                    await _botClient.EditMessageText(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId,
                        text: text,
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                }
                break;

            case "choose_year":
            {
                var currentYear = DateTime.UtcNow.Year;
                var maxHistoryYear = currentYear - 1;

                var baseYear = maxHistoryYear;
                if (int.TryParse(yearValue, out var parsedYear))
                {
                    baseYear = Math.Min(parsedYear, maxHistoryYear);
                }

                var years = Enumerable.Range(baseYear - 4, 5)
                    .Where(y => CalendarService.IsValidYear(y) && y <= maxHistoryYear)
                    .OrderByDescending(y => y)
                    .ToList();

                var buttons = years
                    .Select(y => InlineKeyboardButton.WithCallbackData(
                        y.ToString(),
                        $"mode|set_year|{y}"))
                    .ToList();

                var rows = new List<InlineKeyboardButton[]>();
                for (var i = 0; i < buttons.Count; i += 3)
                {
                    rows.Add(buttons.Skip(i).Take(3).ToArray());
                }

                rows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main")
                });

                var keyboard = new InlineKeyboardMarkup(rows);

                await _botClient.EditMessageText(
                    chatId: message.Chat.Id,
                    messageId: message.MessageId,
                    text: "Select history year:",
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
                break;
            }

            case "set_year" when int.TryParse(yearValue, out var historyYear):
            {
                var currentYear = DateTime.UtcNow.Year;
                var maxHistoryYear = currentYear - 1;

                if (!CalendarService.IsValidYear(historyYear) || historyYear > maxHistoryYear)
                {
                    await _botClient.EditMessageText(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId,
                        text: $"❌ Invalid year {historyYear}.",
                        cancellationToken: cancellationToken);
                    break;
                }

                await _userStateService.SetHistoryModeAsync(userId.Value, historyYear);
                {
                    var state = await _userStateService.GetUserStateAsync(userId.Value);
                    var (_, keyboard) = BuildModeStatusContent(state);
                    await _botClient.EditMessageText(
                        chatId: message.Chat.Id,
                        messageId: message.MessageId,
                        text: $"✅ Switched to History Mode | Year: {historyYear}",
                        replyMarkup: keyboard,
                        cancellationToken: cancellationToken);
                }
                break;
            }
        }
    }

    private static (string Text, InlineKeyboardMarkup Keyboard) BuildModeStatusContent(UserState userState)
    {
        var modeText = userState.IsHistoryMode
            ? $"📊 History Mode | Year: {userState.SelectedYear}"
            : $"📊 Current Mode | Year: {DateTime.UtcNow.Year}";

        var keyboard = userState.IsHistoryMode
            ? new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📅 Current Season", "mode|to_current|"),
                    InlineKeyboardButton.WithCallbackData("📅 Change History Year", $"mode|choose_year|{userState.SelectedYear ?? DateTime.UtcNow.Year}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main")
                }
            })
            : new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📚 Switch to History", $"mode|choose_year|{DateTime.UtcNow.Year}")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🏠 Main Menu", "nav|main")
                }
            });

        return (modeText, keyboard);
    }
}
