using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

// Interface for getting championship standings
public interface IStandingsService
{
    // Returns driver championship standings
    Task<List<DriverStanding>> GetDriverStandingsAsync();

    // Returns constructor/team championship standings
    Task<List<TeamStanding>> GetTeamStandingsAsync();
}