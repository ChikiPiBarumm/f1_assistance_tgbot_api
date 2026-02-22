using System.Linq;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class RaceResultsService : IRaceResultsService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ICalendarService _calendarService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<RaceResultsService> _logger;

    public RaceResultsService(
        IOpenF1Client openF1Client,
        ICalendarService calendarService,
        ISessionService sessionService,
        ILogger<RaceResultsService> logger)
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
                _logger.LogInformation("No race session for meeting_key=latest, searching previous meetings by calendar.");
                var found = await FindLastCompletedRaceSessionAsync();
                if (found == null)
                {
                    _logger.LogWarning("No race session found in calendar fallback.");
                    return new List<RaceResult>();
                }
                return await GetResultsBySessionKeyAsync(found.Value.sessionKey);
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

    public async Task<(List<RaceResult> Results, int MeetingKey, string? RaceName, int? Year)> GetLastRaceResultsWithMeetingKeyAsync()
    {
        try
        {
            const string sessionType = "Race";
            const string meetingKey = "latest";
            _logger.LogInformation("Getting last race results (meeting_key=latest)");

            var sessions = await _openF1Client.GetSessionsAsync(sessionType, meetingKey);

            var latestRaceSession = sessions
                .OrderByDescending(s => s.Date_Start ?? DateTime.MinValue)
                .FirstOrDefault();

            string sessionKey;
            int meetingKeyValue;
            string? raceName = null;
            int? year = null;
            if (latestRaceSession == null)
            {
                _logger.LogInformation("No race session for meeting_key=latest, searching previous meetings by calendar.");
                var found = await FindLastCompletedRaceSessionAsync();
                if (found == null)
                {
                    _logger.LogWarning("No race session found in calendar fallback.");
                    return (new List<RaceResult>(), 0, null, null);
                }
                sessionKey = found.Value.sessionKey;
                meetingKeyValue = found.Value.meetingKey;
                raceName = found.Value.raceName;
                year = found.Value.year;
            }
            else
            {
                _logger.LogDebug("Found race session {SessionKey} for meeting {MeetingKey}", latestRaceSession.Session_Key, latestRaceSession.Meeting_Key);
                sessionKey = latestRaceSession.Session_Key.ToString();
                meetingKeyValue = latestRaceSession.Meeting_Key;
            }

            var results = await GetResultsBySessionKeyAsyncNoDrivers(sessionKey);
            return (results, meetingKeyValue, raceName, year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting last race results with meeting key");
            return (new List<RaceResult>(), 0, null, null);
        }
    }

    public async Task<(int MeetingKey, int Round, int Year)?> GetLastRaceMeetingInfoAsync()
    {
        try
        {
            const string sessionType = "Race";
            const string meetingKeyLatest = "latest";
            _logger.LogInformation("Getting last race meeting info");

            var sessions = await _openF1Client.GetSessionsAsync(sessionType, meetingKeyLatest);
            var latestRaceSession = sessions
                .OrderByDescending(s => s.Date_Start ?? DateTime.MinValue)
                .FirstOrDefault();

            if (latestRaceSession != null)
            {
                var meetingKey = latestRaceSession.Meeting_Key;
                var currentYear = DateTime.UtcNow.Year;
                foreach (var y in new[] { currentYear, currentYear - 1 })
                {
                    var races = await _calendarService.GetRacesAsync(y);
                    var race = races.FirstOrDefault(r => r.Id == meetingKey);
                    if (race != null)
                        return (meetingKey, race.RoundNumber, y);
                }
                _logger.LogWarning("Found session for meeting {MeetingKey} but no matching race in calendar", meetingKey);
                return null;
            }

            _logger.LogInformation("No race session for meeting_key=latest, searching previous meetings by calendar.");
            var found = await FindLastCompletedRaceSessionAsync();
            if (found == null)
            {
                _logger.LogWarning("No race session found in calendar fallback.");
                return null;
            }
            return (found.Value.meetingKey, found.Value.round, found.Value.year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting last race meeting info");
            return null;
        }
    }

    /// <summary>
    /// When meeting_key=latest has no Race session, search backwards through calendar (previous year then current year)
    /// and return the first meeting that has a Race session. Only considers completed races.
    /// Returns race name, year and round from calendar so caller can build heading without another API call.
    /// </summary>
    private async Task<(string sessionKey, int meetingKey, string raceName, int year, int round)?> FindLastCompletedRaceSessionAsync()
    {
        var currentYear = DateTime.UtcNow.Year;
        foreach (var y in new[] { currentYear - 1, currentYear })
        {
            var races = await _calendarService.GetRacesAsync(y);
            var completedRaces = races
                .Where(r => string.Equals(r.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.Date)
                .ToList();
            foreach (var race in completedRaces)
            {
                var sessions = await _openF1Client.GetSessionsAsync("Race", race.Id.ToString());
                var latestSession = sessions
                    .OrderByDescending(s => s.Date_Start ?? DateTime.MinValue)
                    .FirstOrDefault();
                if (latestSession != null)
                {
                    _logger.LogInformation("Found race session for meeting {MeetingKey} (year {Year}, round {Round})", race.Id, y, race.RoundNumber);
                    return (latestSession.Session_Key.ToString(), latestSession.Meeting_Key, race.Name, y, race.RoundNumber);
                }
            }
        }
        return null;
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

    public async Task<List<RaceResult>> GetRaceResultsByMeetingKeyAsync(int meetingKey)
    {
        try
        {
            _logger.LogInformation("Getting race results for meeting {MeetingKey}", meetingKey);

            var sessions = await _openF1Client.GetSessionsAsync("Race", meetingKey.ToString());

            var raceSession = sessions
                .OrderBy(s => s.Date_Start ?? DateTime.MinValue)
                .FirstOrDefault();

            if (raceSession == null)
            {
                _logger.LogWarning("No race session found for meeting {MeetingKey}", meetingKey);
                return new List<RaceResult>();
            }

            var sessionKey = raceSession.Session_Key.ToString();
            return await GetResultsBySessionKeyAsyncNoDrivers(sessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race results for meeting {MeetingKey}", meetingKey);
            return new List<RaceResult>();
        }
    }

    private async Task<List<RaceResult>> GetResultsBySessionKeyAsyncNoDrivers(string sessionKey)
    {
        var results = await _openF1Client.GetSessionResultsAsync(sessionKey);

        if (results.Count == 0)
        {
            _logger.LogWarning("No results found for session {SessionKey}", sessionKey);
            return new List<RaceResult>();
        }

        var mapped = results
            .OrderBy(r => r.Position ?? int.MaxValue)
            .Select(r => new RaceResult
            {
                RaceId = r.Meeting_Key,
                Position = r.Position ?? 0,
                DriverName = $"Driver #{r.Driver_Number}",
                DriverNumber = r.Driver_Number,
                TeamName = "-",
                Points = (int)r.Points,
                Status = string.IsNullOrWhiteSpace(r.Status) ? "Finished" : r.Status
            })
            .ToList();

        _logger.LogInformation("Successfully retrieved {Count} race results by meeting key", mapped.Count);
        return mapped;
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
            .OrderBy(r => r.Position ?? int.MaxValue)
            .Select(r =>
            {
                var position = r.Position ?? 0;
                if (driverLookup.TryGetValue(r.Driver_Number, out var driverInfo))
                {
                    return new RaceResult
                    {
                        RaceId = r.Meeting_Key,
                        Position = position,
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
                    Position = position,
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
