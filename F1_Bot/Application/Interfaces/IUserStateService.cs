using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface IUserStateService
{
    Task SetHistoryModeAsync(long userId, int year);
    Task SetCurrentModeAsync(long userId);
    Task<UserState> GetUserStateAsync(long userId);
}
