using F1_Bot.Services;
using Microsoft.Extensions.Logging;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot.Handlers;

public class StartHandler : IStartHandler
{
    private readonly MessageSender _messageSender;
    private readonly IUserStateService _userStateService;
    private readonly ILogger<StartHandler> _logger;

    public StartHandler(
        MessageSender messageSender,
        IUserStateService userStateService,
        ILogger<StartHandler> logger)
    {
        _messageSender = messageSender;
        _userStateService = userStateService;
        _logger = logger;
    }

    public async Task HandleStartAsync(Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var userState = await _userStateService.GetUserStateAsync(userId);
        var effectiveYear = await _userStateService.GetEffectiveYearAsync(userId);

        var welcomeText = @"🏎️ Welcome to F1 Assistance Bot!

I can help you with Formula 1 information for the current season and past years. Use the buttons below to browse races, standings, and results.

To change season: tap the Season button (shows the current year). You can switch to the current season or pick a past year for historical data.";

        var quickActionsRows = userState.IsHistoryMode
            ? new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 Calendar", "nav|calendar"), InlineKeyboardButton.WithCallbackData("📊 Standings", "nav|standings") },
                new[] { InlineKeyboardButton.WithCallbackData($"⚙️ Season {effectiveYear}", "nav|mode"), InlineKeyboardButton.WithCallbackData("ℹ️ Help", "nav|help") }
            }
            : new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📅 Calendar", "nav|calendar"), InlineKeyboardButton.WithCallbackData("🏁 Next Race", "nav|next") },
                new[] { InlineKeyboardButton.WithCallbackData("📊 Standings", "nav|standings"), InlineKeyboardButton.WithCallbackData("🏆 Last Race Info", "nav|results") },
                new[] { InlineKeyboardButton.WithCallbackData($"⚙️ Season {effectiveYear}", "nav|mode"), InlineKeyboardButton.WithCallbackData("ℹ️ Help", "nav|help") }
            };
        var quickActionsKeyboard = new InlineKeyboardMarkup(quickActionsRows);

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            welcomeText,
            quickActionsKeyboard,
            cancellationToken: cancellationToken);
    }

    public async Task HandleHelpAsync(Message message, CancellationToken cancellationToken)
    {
        var helpText = @"📖 Main buttons

📅 Calendar — Full race calendar for the selected season. Pick a round for details, schedule, and results.

🏁 Next Race — Next upcoming race: schedule, details, and (when available) results.

📊 Standings — Driver and constructor standings. You can view by round or switch season.

🏆 Last Race Info — Opens the last race details (same as picking a race from the calendar). From there you can open results, standings, or return to calendar.

⚙️ Season (year) — Current season or history. Tap to switch to current season or choose a past year.

🏠 Main Menu — Return to this welcome screen and main options.";

        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            helpText,
            cancellationToken: cancellationToken);
    }

    public async Task HandleUnknownAsync(Message message, CancellationToken cancellationToken)
    {
        await _messageSender.SendMessageAsync(
            message.Chat.Id,
            "❓ Unknown command. Use /help to see available commands.",
            cancellationToken: cancellationToken);
    }
}
