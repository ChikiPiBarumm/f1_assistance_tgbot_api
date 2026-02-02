using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot.Handlers;

public interface ICalendarHandler
{
    Task HandleCalendarCommandAsync(Message message, string[] arguments, CancellationToken cancellationToken);
    Task HandleCalendarCallbackAsync(Message message, string action, string? yearValue, string? roundValue, string? meetingKeyValue, CancellationToken cancellationToken);
}
