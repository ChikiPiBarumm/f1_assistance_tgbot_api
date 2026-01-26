using F1_Bot.Domain.Models;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class UserStateService : IUserStateService
{
    private readonly Dictionary<long, UserState> _userStates = new();
    private readonly ILogger<UserStateService> _logger;
    private readonly object _lock = new();

    public UserStateService(ILogger<UserStateService> logger)
    {
        _logger = logger;
    }

    public Task SetHistoryModeAsync(long userId, int year)
    {
        lock (_lock)
        {
            _userStates[userId] = new UserState
            {
                IsHistoryMode = true,
                SelectedYear = year
            };
            _logger.LogDebug("User {UserId} switched to history mode for year {Year}", userId, year);
        }
        return Task.CompletedTask;
    }

    public Task SetCurrentModeAsync(long userId)
    {
        lock (_lock)
        {
            _userStates[userId] = new UserState
            {
                IsHistoryMode = false,
                SelectedYear = null
            };
            _logger.LogDebug("User {UserId} switched to current mode", userId);
        }
        return Task.CompletedTask;
    }

    public Task<UserState> GetUserStateAsync(long userId)
    {
        lock (_lock)
        {
            if (!_userStates.TryGetValue(userId, out var state))
            {
                state = new UserState
                {
                    IsHistoryMode = false,
                    SelectedYear = null
                };
                _userStates[userId] = state;
            }
            return Task.FromResult(state);
        }
    }
}
