using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace F1_Bot.Presentation.Bot;

public class MessageSender
{
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<MessageSender> _logger;

    public MessageSender(
        ITelegramBotClient botClient,
        ILogger<MessageSender> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task SendMessageAsync(
        ChatId chatId,
        string text,
        CancellationToken cancellationToken = default)
    {
        await SendMessageInternalAsync(chatId, text, null, cancellationToken);
    }

    public async Task SendMessageAsync(
        ChatId chatId,
        string text,
        ReplyMarkup? replyMarkup,
        CancellationToken cancellationToken = default)
    {
        await SendMessageInternalAsync(chatId, text, replyMarkup, cancellationToken);
    }

    private async Task SendMessageInternalAsync(
        ChatId chatId,
        string text,
        ReplyMarkup? replyMarkup,
        CancellationToken cancellationToken)
    {
        const int maxRetries = 3;
        const int timeoutSeconds = 15;

        var startTime = DateTimeOffset.UtcNow;
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
                var linkedCts = cancellationToken.CanBeCanceled && !cancellationToken.IsCancellationRequested
                    ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token)
                    : timeoutCts;

                await _botClient.SendMessage(chatId, text, replyMarkup: replyMarkup, cancellationToken: linkedCts.Token);
                return;
            }
            catch (OperationCanceledException) when (attempt < maxRetries)
            {
                var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning("SendMessage timeout on attempt {Attempt}/{MaxRetries} after {Elapsed}ms, retrying...", attempt, maxRetries, elapsed);
                
                var delayMs = 500 * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries && (ex is HttpRequestException || ex is TaskCanceledException))
            {
                var elapsed = (DateTimeOffset.UtcNow - startTime).TotalMilliseconds;
                _logger.LogWarning(ex, "SendMessage error on attempt {Attempt}/{MaxRetries} after {Elapsed}ms, retrying...", attempt, maxRetries, elapsed);
                
                var delayMs = 500 * (int)Math.Pow(2, attempt - 1);
                await Task.Delay(TimeSpan.FromMilliseconds(delayMs), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SendMessage failed after {Attempts} attempts", attempt);
                throw;
            }
        }
    }
}
