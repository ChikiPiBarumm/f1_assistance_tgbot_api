using F1_Bot.Application.Interfaces;
using F1_Bot.Domain.Constants;

namespace F1_Bot.Presentation.Bot;

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
                if (SeasonConstants.IsValidYear(value))
                {
                    year = value;
                }
                else if (value >= 1 && value <= SeasonConstants.MaxRoundsPerSeason)
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
            var firstParsed = int.TryParse(arguments[0], out var first);
            int secondVal = 0;
            var secondParsed = !string.IsNullOrWhiteSpace(arguments[1]) && int.TryParse(arguments[1], out secondVal);

            if (firstParsed && SeasonConstants.IsValidYear(first))
            {
                year = first;
                if (secondParsed)
                {
                    if (secondVal >= 1 && secondVal <= SeasonConstants.MaxRoundsPerSeason)
                        round = secondVal;
                    else if (SeasonConstants.IsValidYear(secondVal))
                    {
                        year = secondVal;
                        round = first >= 1 && first <= SeasonConstants.MaxRoundsPerSeason ? first : null;
                    }
                }
            }
            else if (firstParsed && secondParsed && SeasonConstants.IsValidYear(secondVal))
            {
                year = secondVal;
                round = first >= 1 && first <= SeasonConstants.MaxRoundsPerSeason ? first : null;
            }
        }

        return (year, round);
    }
}
