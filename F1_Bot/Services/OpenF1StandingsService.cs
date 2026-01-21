using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class OpenF1StandingsService : IStandingsService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ILogger<OpenF1StandingsService> _logger;

    public OpenF1StandingsService(IOpenF1Client openF1Client, ILogger<OpenF1StandingsService> logger)
    {
        _openF1Client = openF1Client;
        _logger = logger;
    }

    public async Task<List<DriverStanding>> GetDriverStandingsAsync()
    {
        try
        {
            const string sessionKey = "latest";
            _logger.LogInformation("Getting driver standings");

            var championship = await _openF1Client.GetDriverChampionshipAsync(sessionKey);
            var drivers = await _openF1Client.GetDriversAsync(sessionKey);

            if (championship.Count == 0)
            {
                _logger.LogWarning("No championship data found for session {SessionKey}", sessionKey);
                return new List<DriverStanding>();
            }

            var driverLookup = drivers
                .GroupBy(d => d.Driver_Number)
                .ToDictionary(g => g.Key, g => g.First());

            var standings = championship
                .OrderBy(d => d.Position_Current)
                .Select(d =>
                {
                    if (driverLookup.TryGetValue(d.Driver_Number, out var driverInfo))
                    {
                        return new DriverStanding
                        {
                            Position = d.Position_Current,
                            DriverName = driverInfo.Full_Name,
                            DriverNumber = d.Driver_Number,
                            TeamName = driverInfo.Team_Name,
                            Points = (int)d.Points_Current
                        };
                    }

                    _logger.LogWarning("Driver #{DriverNumber} not found in drivers list", d.Driver_Number);
                    return new DriverStanding
                    {
                        Position = d.Position_Current,
                        DriverName = $"Driver #{d.Driver_Number}",
                        DriverNumber = d.Driver_Number,
                        TeamName = "Unknown Team",
                        Points = (int)d.Points_Current
                    };
                })
                .ToList();

            _logger.LogInformation("Successfully retrieved {Count} driver standings", standings.Count);
            return standings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting driver standings");
            return new List<DriverStanding>();
        }
    }

    public async Task<List<TeamStanding>> GetTeamStandingsAsync()
    {
        try
        {
            const string sessionKey = "latest";
            _logger.LogInformation("Getting team standings");

            var openF1Teams = await _openF1Client.GetTeamChampionshipAsync(sessionKey);

            if (openF1Teams.Count == 0)
            {
                _logger.LogWarning("No team championship data found for session {SessionKey}", sessionKey);
                return new List<TeamStanding>();
            }

            var standings = openF1Teams
                .OrderBy(t => t.Position_Current)
                .Select(t => new TeamStanding
                {
                    Position = t.Position_Current,
                    TeamName = t.Team_Name,
                    Points = (int)t.Points_Current
                })
                .ToList();

            _logger.LogInformation("Successfully retrieved {Count} team standings", standings.Count);
            return standings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting team standings");
            return new List<TeamStanding>();
        }
    }
}