using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot.Handlers;

public interface IResultsHandler
{
    Task HandleLastRaceResultsAsync(Message message, string[] arguments, CancellationToken cancellationToken);
    Task HandleResultsByMeetingKeyAsync(Message message, int meetingKey, int year, int round, CancellationToken cancellationToken);
    Task HandleResultsCallbackAsync(Message message, string action, string? yearValue, string? roundValue, CancellationToken cancellationToken);
}
