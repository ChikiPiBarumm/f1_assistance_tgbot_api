namespace F1_Bot.Presentation.Bot;

public interface IArgumentParser
{
    Task<(int? year, int? round)> ParseYearRoundAsync(string[] arguments, long userId, CancellationToken cancellationToken = default);
}
