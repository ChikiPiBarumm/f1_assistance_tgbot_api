using System.Linq;
using F1_Bot.Application.Interfaces;
using F1_Bot.Domain.Constants;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Application;

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

            var latest = await GetLatestRaceSessionInfoAsync();
            if (latest == null)
            {
                _logger.LogWarning("No race session found for latest race.");
                return new List<RaceResult>();
            }
            return await GetResultsBySessionKeyAsync(latest.Value.sessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting last race results");
            return new List<RaceResult>();
        }
    }

    public async Task<(int MeetingKey, int Round, int Year)?> GetLastRaceMeetingInfoAsync()
    {
        try
        {
            _logger.LogInformation("Getting last race meeting info");
            var latest = await GetLatestRaceSessionInfoAsync();
            if (latest == null)
            {
                _logger.LogWarning("No race session found for latest race.");
                return null;
            }
            return (latest.Value.meetingKey, latest.Value.round, latest.Value.year);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting last race meeting info");
            return null;
        }
    }

    /// <summary>
    /// Resolves the latest race session: tries OpenF1 meeting_key=latest, then falls back to calendar (last completed race).
    /// Returns session key, meeting key, round and year for building results or headings.
    /// </summary>
    private async Task<(string sessionKey, int meetingKey, int round, int year)?> GetLatestRaceSessionInfoAsync()
    {
        var sessions = await _openF1Client.GetSessionsAsync(OpenF1SessionName.Race, "latest");
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
                {
                    _logger.LogDebug("Found race session {SessionKey} for meeting {MeetingKey}", latestRaceSession.Session_Key, meetingKey);
                    return (latestRaceSession.Session_Key.ToString(), meetingKey, race.RoundNumber, y);
                }
            }
            _logger.LogWarning("Found session for meeting {MeetingKey} but no matching race in calendar", meetingKey);
            return null;
        }

        _logger.LogInformation("No race session for meeting_key=latest, searching previous meetings by calendar.");
        var found = await FindLastCompletedRaceSessionAsync();
        if (found == null)
            return null;
        return (found.Value.sessionKey, found.Value.meetingKey, found.Value.round, found.Value.year);
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
                .Where(r => string.Equals(r.Status, RaceStatus.Completed, StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(r => r.Date)
                .ToList();
            foreach (var race in completedRaces)
            {
                var sessionKey = await _sessionService.GetRaceSessionKeyAsync(race.Id);
                if (!string.IsNullOrEmpty(sessionKey))
                {
                    _logger.LogInformation("Found race session for meeting {MeetingKey} (year {Year}, round {Round})", race.Id, y, race.RoundNumber);
                    return (sessionKey, race.Id, race.Name, y, race.RoundNumber);
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

            var sessionKey = await _sessionService.GetRaceSessionKeyAsync(race.Id);
            if (string.IsNullOrEmpty(sessionKey))
            {
                _logger.LogWarning("No race session found for round {Round}, meeting {MeetingKey}", round, race.Id);
                return new List<RaceResult>();
            }

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

            var sessionKey = await _sessionService.GetRaceSessionKeyAsync(meetingKey);
            if (string.IsNullOrEmpty(sessionKey))
            {
                _logger.LogWarning("No race session found for meeting {MeetingKey}", meetingKey);
                return new List<RaceResult>();
            }

            return await GetResultsBySessionKeyAsync(sessionKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race results for meeting {MeetingKey}", meetingKey);
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
