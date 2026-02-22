using F1_Bot.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace F1_Bot.Presentation.Bot;

public class PollingService : BackgroundService, ITelegramBotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ILogger<PollingService> _logger;

    public PollingService(
        ITelegramBotClient botClient,
        IServiceScopeFactory serviceScopeFactory,
        ILogger<PollingService> logger)
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
        if (exception is Telegram.Bot.Exceptions.RequestException requestEx &&
            (requestEx.InnerException is TaskCanceledException || 
             requestEx.InnerException is TimeoutException ||
             requestEx.Message.Contains("timed out", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogDebug("Polling timeout (this is normal for long polling): {Message}", requestEx.Message);
        }
        else
        {
            _logger.LogError(exception, "Error while polling for updates");
        }
        return Task.CompletedTask;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Starting Telegram bot background service...");

        await WarmUpCalendarCacheAsync(cancellationToken);

        await base.StartAsync(cancellationToken);
    }

    private async Task WarmUpCalendarCacheAsync(CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _serviceScopeFactory.CreateScope();
            var calendarService = scope.ServiceProvider.GetRequiredService<ICalendarService>();
            var year = DateTime.UtcNow.Year;
            _logger.LogInformation("Pre-warming calendar cache for year {Year} and {PrevYear}...", year, year - 1);
            await calendarService.GetRacesAsync(year);
            await calendarService.GetRacesAsync(year - 1);
            _logger.LogInformation("Calendar cache pre-warmed.");
        }
        catch (OperationCanceledException)
        {
            // Ignore when app is shutting down
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Calendar cache pre-warm failed (first Last Race Info may be slower).");
        }
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping Telegram bot...");
        return base.StopAsync(cancellationToken);
    }
}
