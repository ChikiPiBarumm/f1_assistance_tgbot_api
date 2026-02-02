using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot.Handlers;

public interface IStandingsHandler
{
    Task ShowStandingsChoiceAsync(Message message, CancellationToken cancellationToken);
    Task HandleDriverStandingsAsync(Message message, string[] arguments, CancellationToken cancellationToken);
    Task HandleTeamStandingsAsync(Message message, string[] arguments, CancellationToken cancellationToken);
    Task HandleStandingsCallbackAsync(Message message, string action, string? yearValue, string? roundValue, CancellationToken cancellationToken);
}
