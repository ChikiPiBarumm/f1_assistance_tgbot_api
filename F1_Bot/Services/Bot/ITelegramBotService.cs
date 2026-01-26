namespace F1_Bot.Services.Bot;

public interface ITelegramBotService
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
