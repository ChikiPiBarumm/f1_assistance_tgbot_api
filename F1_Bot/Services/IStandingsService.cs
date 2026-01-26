using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface IStandingsService
{
    Task<List<DriverStanding>> GetDriverStandingsAsync();
    Task<List<TeamStanding>> GetTeamStandingsAsync();
}