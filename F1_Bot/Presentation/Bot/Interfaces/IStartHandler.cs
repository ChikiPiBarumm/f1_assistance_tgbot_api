using Telegram.Bot.Types;

namespace F1_Bot.Presentation.Bot.Handlers;

public interface IStartHandler
{
    Task HandleStartAsync(Message message, CancellationToken cancellationToken);
    Task HandleHelpAsync(Message message, CancellationToken cancellationToken);
    Task HandleUnknownAsync(Message message, CancellationToken cancellationToken);
}
