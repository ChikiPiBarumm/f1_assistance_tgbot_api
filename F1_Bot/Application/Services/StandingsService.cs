using System.Linq;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class StandingsService : IStandingsService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ICalendarService _calendarService;
    private readonly ISessionService _sessionService;
    private readonly ILogger<StandingsService> _logger;

    public StandingsService(
        IOpenF1Client openF1Client,
        ICalendarService calendarService,
        ISessionService sessionService,
        ILogger<StandingsService> logger)
    {
        _openF1Client = openF1Client;
        _calendarService = calendarService;
        _sessionService = sessionService;
        _logger = logger;
    }

    public async Task<List<DriverStanding>> GetDriverStandingsAsync(int? year = null, int? round = null)
    {
        try
        {
            string? sessionKey = null;

            if (year.HasValue)
            {
                _logger.LogInformation("Getting driver standings for year {Year}, round {Round}", year, round);
                sessionKey = await GetSessionKeyForYearRoundAsync(year.Value, round);
            }
            else
            {
                sessionKey = "latest";
                _logger.LogInformation("Getting driver standings (latest)");
            }

            if (string.IsNullOrEmpty(sessionKey))
            {
                _logger.LogWarning("No session key found for year {Year}, round {Round}", year, round);
                return new List<DriverStanding>();
            }

            var championship = await _openF1Client.GetDriverChampionshipAsync(sessionKey);

            if (championship.Count == 0)
            {
                _logger.LogWarning("No championship data found for session {SessionKey}", sessionKey);
                return new List<DriverStanding>();
            }

            int? meetingKey = null;
            if (year.HasValue)
            {
                var races = await _calendarService.GetRacesAsync(year);
                Race? targetRace = round.HasValue
                    ? races.FirstOrDefault(r => r.RoundNumber == round.Value)
                    : races.OrderByDescending(r => r.Date).FirstOrDefault();
                meetingKey = targetRace?.Id;
            }

            var drivers = await GetDriversWithFallbackAsync(sessionKey, meetingKey);

            var driverLookup = drivers
                .GroupBy(d => d.Driver_Number)
                .ToDictionary(g => g.Key, g => g.First());

            var standings = championship
                .OrderBy(d => d.Position_Current)
                .Take(20)
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

    public async Task<List<TeamStanding>> GetTeamStandingsAsync(int? year = null, int? round = null)
    {
        try
        {
            string? sessionKey = null;

            if (year.HasValue)
            {
                _logger.LogInformation("Getting team standings for year {Year}, round {Round}", year, round);
                sessionKey = await GetSessionKeyForYearRoundAsync(year.Value, round);
            }
            else
            {
                sessionKey = "latest";
                _logger.LogInformation("Getting team standings (latest)");
            }

            if (string.IsNullOrEmpty(sessionKey))
            {
                _logger.LogWarning("No session key found for year {Year}, round {Round}", year, round);
                return new List<TeamStanding>();
            }

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

    private async Task<string?> GetSessionKeyForYearRoundAsync(int year, int? round)
    {
        try
        {
            var races = await _calendarService.GetRacesAsync(year);

            if (races.Count == 0)
            {
                _logger.LogWarning("No races found for year {Year}", year);
                return null;
            }

            Race? targetRace;

            if (round.HasValue)
            {
                targetRace = races.FirstOrDefault(r => r.RoundNumber == round.Value);
                if (targetRace == null)
                {
                    _logger.LogWarning("Round {Round} not found for year {Year}", round, year);
                    return null;
                }
            }
            else
            {
                targetRace = races.OrderByDescending(r => r.Date).FirstOrDefault();
                if (targetRace == null)
                {
                    _logger.LogWarning("No races found for year {Year}", year);
                    return null;
                }
            }

            return await _sessionService.GetRaceSessionKeyAsync(targetRace.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting session key for year {Year}, round {Round}", year, round);
            return null;
        }
    }

    private async Task<List<OpenF1DriverDto>> GetDriversWithFallbackAsync(string sessionKey, int? meetingKey)
    {
        var drivers = await _openF1Client.GetDriversAsync(sessionKey);

        if (drivers.Count > 0)
        {
            _logger.LogDebug("Found {Count} drivers for session {SessionKey}", drivers.Count, sessionKey);
            return drivers;
        }

        if (!meetingKey.HasValue)
        {
            _logger.LogWarning("No drivers found for session {SessionKey} and no meeting key provided for fallback", sessionKey);
            return drivers;
        }

        _logger.LogDebug("No drivers found for race session {SessionKey}, trying fallback sessions for meeting {MeetingKey}", sessionKey, meetingKey);

        var fallbackSessionTypes = new[] { "Qualifying", "Practice 1", "Practice 2", "Practice 3", "FP1", "FP2", "FP3" };

        foreach (var sessionType in fallbackSessionTypes)
        {
            var sessions = await _openF1Client.GetSessionsAsync(sessionType, meetingKey.Value.ToString());
            if (sessions.Count > 0)
            {
                var fallbackSession = sessions.OrderByDescending(s => s.Date_Start ?? DateTime.MinValue).FirstOrDefault();
                if (fallbackSession != null)
                {
                    var fallbackDrivers = await _openF1Client.GetDriversAsync(fallbackSession.Session_Key.ToString());
                    if (fallbackDrivers.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} drivers from fallback session {SessionType} (session_key: {FallbackSessionKey})", 
                            fallbackDrivers.Count, sessionType, fallbackSession.Session_Key);
                        return fallbackDrivers;
                    }
                }
            }
        }

        var allSessions = await _openF1Client.GetSessionsAsync("", meetingKey.Value.ToString());
        foreach (var session in allSessions.OrderByDescending(s => s.Date_Start ?? DateTime.MinValue))
        {
            if (session.Session_Type != "Race")
            {
                var fallbackDrivers = await _openF1Client.GetDriversAsync(session.Session_Key.ToString());
                if (fallbackDrivers.Count > 0)
                {
                    _logger.LogInformation("Found {Count} drivers from fallback session {SessionType} (session_key: {FallbackSessionKey})", 
                        fallbackDrivers.Count, session.Session_Type, session.Session_Key);
                    return fallbackDrivers;
                }
            }
        }

        _logger.LogWarning("No drivers found even after trying fallback sessions for meeting {MeetingKey}", meetingKey);
        return drivers;
    }
}
