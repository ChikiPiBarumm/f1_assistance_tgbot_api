using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class OpenF1RaceResultsService : IRaceResultsService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ILogger<OpenF1RaceResultsService> _logger;

    public OpenF1RaceResultsService(IOpenF1Client openF1Client, ILogger<OpenF1RaceResultsService> logger)
    {
        _openF1Client = openF1Client;
        _logger = logger;
    }

    public async Task<List<RaceResult>> GetLastRaceResultsAsync()
    {
        try
        {
            const string sessionType = "Race";
            const string meetingKey = "latest";
            _logger.LogInformation("Getting last race results");

            var sessions = await _openF1Client.GetSessionsAsync(sessionType, meetingKey);

            var latestRaceSession = sessions
                .OrderByDescending(s => s.Date_Start ?? DateTime.MinValue)
                .FirstOrDefault();

            if (latestRaceSession == null)
            {
                _logger.LogWarning("No race session found for type={SessionType}, meeting={MeetingKey}", sessionType, meetingKey);
                return new List<RaceResult>();
            }

            _logger.LogDebug("Found race session {SessionKey} for meeting {MeetingKey}", latestRaceSession.Session_Key, latestRaceSession.Meeting_Key);

            var sessionKey = latestRaceSession.Session_Key.ToString();
            var results = await _openF1Client.GetSessionResultsAsync(sessionKey);
            var drivers = await _openF1Client.GetDriversAsync(sessionKey);

            if (results.Count == 0)
            {
                _logger.LogWarning("No results found for session {SessionKey}", sessionKey);
                return new List<RaceResult>();
            }

            var driverLookup = drivers
                .GroupBy(d => d.Driver_Number)
                .ToDictionary(g => g.Key, g => g.First());

            var mapped = results
                .OrderBy(r => r.Position)
                .Select(r =>
                {
                    if (driverLookup.TryGetValue(r.Driver_Number, out var driverInfo))
                    {
                        return new RaceResult
                        {
                            RaceId = r.Meeting_Key,
                            Position = r.Position,
                            DriverName = driverInfo.Full_Name,
                            DriverNumber = r.Driver_Number,
                            TeamName = driverInfo.Team_Name,
                            Points = (int)r.Points,
                            Status = string.IsNullOrWhiteSpace(r.Status) ? "Finished" : r.Status
                        };
                    }

                    _logger.LogWarning("Driver #{DriverNumber} not found in drivers list for race results", r.Driver_Number);
                    return new RaceResult
                    {
                        RaceId = r.Meeting_Key,
                        Position = r.Position,
                        DriverName = $"Driver #{r.Driver_Number}",
                        DriverNumber = r.Driver_Number,
                        TeamName = "Unknown Team",
                        Points = (int)r.Points,
                        Status = string.IsNullOrWhiteSpace(r.Status) ? "Finished" : r.Status
                    };
                })
                .ToList();

            _logger.LogInformation("Successfully retrieved {Count} race results", mapped.Count);
            return mapped;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting last race results");
            return new List<RaceResult>();
        }
    }
}