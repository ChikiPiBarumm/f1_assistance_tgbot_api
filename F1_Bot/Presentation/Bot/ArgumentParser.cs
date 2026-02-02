using F1_Bot.Services;

namespace F1_Bot.Presentation.Bot;

public interface IArgumentParser
{
    Task<(int? year, int? round)> ParseYearRoundAsync(string[] arguments, long userId, CancellationToken cancellationToken = default);
}

public class ArgumentParser : IArgumentParser
{
    private readonly IUserStateService _userStateService;

    public ArgumentParser(IUserStateService userStateService)
    {
        _userStateService = userStateService;
    }

    public async Task<(int? year, int? round)> ParseYearRoundAsync(string[] arguments, long userId, CancellationToken cancellationToken = default)
    {
        int? year = null;
        int? round = null;

        if (arguments.Length == 0)
        {
            var userState = await _userStateService.GetUserStateAsync(userId);
            if (userState.IsHistoryMode && userState.SelectedYear.HasValue)
            {
                year = userState.SelectedYear.Value;
            }
            return (year, round);
        }

        if (arguments.Length == 1)
        {
            if (int.TryParse(arguments[0], out var value))
            {
                if (value >= 1950 && value <= DateTime.UtcNow.Year + 1)
                {
                    year = value;
                }
                else if (value >= 1 && value <= 24)
                {
                    round = value;
                    var userState = await _userStateService.GetUserStateAsync(userId);
                    if (userState.IsHistoryMode && userState.SelectedYear.HasValue)
                    {
                        year = userState.SelectedYear.Value;
                    }
                }
            }
        }
        else if (arguments.Length >= 2)
        {
            if (int.TryParse(arguments[0], out var first) && int.TryParse(arguments[1], out var second))
            {
                if (first >= 1950 && first <= DateTime.UtcNow.Year + 1)
                {
                    year = first;
                    round = second;
                }
                else if (second >= 1950 && second <= DateTime.UtcNow.Year + 1)
                {
                    round = first;
                    year = second;
                }
            }
        }

        return (year, round);
    }
}
