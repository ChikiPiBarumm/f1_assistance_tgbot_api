using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface IStandingsService
{
    Task<List<DriverStanding>> GetDriverStandingsAsync(int? year = null, int? round = null);
    Task<List<TeamStanding>> GetTeamStandingsAsync(int? year = null, int? round = null);
}
