using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;

namespace F1_Bot.Services;

// Standings service that uses OpenF1 API
public class OpenF1StandingsService : IStandingsService
{
    private readonly IOpenF1Client _openF1Client;

    public OpenF1StandingsService(IOpenF1Client openF1Client)
    {
        _openF1Client = openF1Client;
    }

    public async Task<List<DriverStanding>> GetDriverStandingsAsync()
    {
        const string sessionKey = "latest";
        
        // We'll assume "latest" session gives us current championship standings.
        var championship = await _openF1Client.GetDriverChampionshipAsync("latest");

        // Get drivers list for the current year to enrich names and team names
        var drivers = await _openF1Client.GetDriversAsync(sessionKey);

        // Build a lookup dictionary: driver_number -> driver DTO
        var driverLookup = drivers
            .GroupBy(d => d.Driver_Number)
            .ToDictionary(g => g.Key, g => g.First());

        // Map championship data to our domain model, using driver info when available
        var standings = championship
            .OrderBy(d => d.Position_Current)
            .Select(d =>
            {
                // Try to find matching driver info
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

                // Fallback if driver info is missing
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

        return standings;
    }

    public async Task<List<TeamStanding>> GetTeamStandingsAsync()
    {
        var openF1Teams = await _openF1Client.GetTeamChampionshipAsync("latest");

        var standings = openF1Teams
            .OrderBy(t => t.Position_Current)
            .Select(t => new TeamStanding
            {
                Position = t.Position_Current,
                TeamName = t.Team_Name,
                Points = (int)t.Points_Current
            })
            .ToList();

        return standings;
    }
}