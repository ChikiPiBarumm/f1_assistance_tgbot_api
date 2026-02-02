using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot.Handlers;

public interface IModeHandler
{
    Task HandleHistoryAsync(Message message, string[] arguments, CancellationToken cancellationToken);
    Task HandleCurrentAsync(Message message, CancellationToken cancellationToken);
    Task HandleModeAsync(Message message, CancellationToken cancellationToken);
    Task HandleModeCallbackAsync(Message message, string action, string? yearValue, CancellationToken cancellationToken);
}
