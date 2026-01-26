using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace F1_Bot.Services.Bot;

public class TelegramBotPollingService : BackgroundService, ITelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<TelegramBotPollingService> _logger;

    public TelegramBotPollingService(
        ITelegramBotClient botClient,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<TelegramBotPollingService> logger)
    {
        _botClient = botClient;
        _serviceScopeFactory = serviceScopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting Telegram bot with long polling...");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = Array.Empty<UpdateType>()
        };

        _botClient.StartReceiving(
            updateHandler: HandleUpdateAsync,
            errorHandler: HandlePollingErrorAsync,
            receiverOptions: receiverOptions,
            cancellationToken: stoppingToken
        );

        try
        {
            _logger.LogInformation("Testing connectivity to Telegram API...");
            var me = await _botClient.GetMe(stoppingToken);
            _logger.LogInformation("Bot @{BotUsername} is running and waiting for messages...", me.Username);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to connect to Telegram API. Check your network connection and bot token.");
            throw;
        }

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var commandRouter = scope.ServiceProvider.GetRequiredService<TelegramBotCommandRouter>();
        
        try
        {
            await commandRouter.HandleUpdateAsync(update, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling update {UpdateId}", update.Id);
        }
    }

    private Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Error while polling for updates");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping Telegram bot...");
        return Task.CompletedTask;
    }
}