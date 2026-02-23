using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot;

/// <summary>
/// Shared helpers for Telegram bot operations (e.g. best-effort delete with logging).
/// </summary>
public static class TelegramBotHelpers
{
    /// <summary>
    /// Attempts to delete a message. Logs a warning and does not throw if the delete fails (e.g. message already deleted).
    /// </summary>
    public static async Task TryDeleteMessageAsync(
        ITelegramBotClient botClient,
        ChatId chatId,
        int messageId,
        ILogger logger,
        string context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await botClient.DeleteMessage(chatId, messageId, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not delete {Context}", context);
        }
    }
}
