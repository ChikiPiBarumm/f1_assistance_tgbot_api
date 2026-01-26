using System.Linq;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class OpenF1RaceResultsService : IRaceResultsService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ICalendarService _calendarService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<OpenF1RaceResultsService> _logger;

    public OpenF1RaceResultsService(
        IOpenF1Client openF1Client,
        ICalendarService calendarService,
        ISessionService sessionService,
        ILogger<OpenF1RaceResultsService> logger)
    {
        _openF1Client = openF1Client;
        _calendarService = calendarService;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<List<RaceResult>> GetLastRaceResultsAsync(int? year = null)
    {
        try
        {
            if (year.HasValue)
            {
                _logger.LogInformation("Getting last race results for year {Year}", year);
                var races = await _calendarService.GetRacesAsync(year);
                var lastRace = races.OrderByDescending(r => r.Date).FirstOrDefault();

                if (lastRace == null)
                {
                    _logger.LogWarning("No races found for year {Year}", year);
                    return new List<RaceResult>();
                }

                var sessionKey = await _sessionService.GetRaceSessionKeyAsync(lastRace.Id);
                if (string.IsNullOrEmpty(sessionKey))
                {
                    _logger.LogWarning("No session key found for last race of year {Year}", year);
                    return new List<RaceResult>();
                }

                return await GetResultsBySessionKeyAsync(sessionKey);
            }

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

            var latestSessionKey = latestRaceSession.Session_Key.ToString();
            return await GetResultsBySessionKeyAsync(latestSessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting last race results");
            return new List<RaceResult>();
        }
    }

    public async Task<List<RaceResult>> GetRaceResultsByRoundAsync(int round, int? year = null)
    {
        try
        {
            _logger.LogInformation("Getting race results for round {Round}, year {Year}", round, year ?? DateTime.UtcNow.Year);

            var races = await _calendarService.GetRacesAsync(year);
            var race = races.FirstOrDefault(r => r.RoundNumber == round);

            if (race == null)
            {
                _logger.LogWarning("Race not found for round {Round}", round);
                return new List<RaceResult>();
            }

            var sessions = await _openF1Client.GetSessionsAsync("Race", race.Id.ToString());

            var raceSession = sessions
                .OrderByDescending(s => s.Date_Start ?? DateTime.MinValue)
                .FirstOrDefault();

            if (raceSession == null)
            {
                _logger.LogWarning("No race session found for round {Round}, meeting {MeetingKey}", round, race.Id);
                return new List<RaceResult>();
            }

            var sessionKey = raceSession.Session_Key.ToString();
            return await GetResultsBySessionKeyAsync(sessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race results for round {Round}", round);
            return new List<RaceResult>();
        }
    }

    private async Task<List<RaceResult>> GetResultsBySessionKeyAsync(string sessionKey)
    {
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
}
