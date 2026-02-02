using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot.Handlers;

public interface IRaceDetailsHandler
{
    Task HandleNextRaceAsync(Message message, string[] arguments, CancellationToken cancellationToken);
    Task HandleRaceDetailsAsync(Message message, string[] arguments, CancellationToken cancellationToken);
    Task HandleRaceDetailsByMeetingKeyAsync(Message message, int meetingKey, int round, int year, CancellationToken cancellationToken, bool fromNextRace = false);
    Task EditRaceDetailsByMeetingKeyAsync(Message message, int meetingKey, int round, int year, CancellationToken cancellationToken);
    Task HandleRaceCallbackAsync(Message message, string action, string? yearValue, string? roundValue, string? meetingKeyValue, CancellationToken cancellationToken);
}
