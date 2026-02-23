using System.Linq;
using F1_Bot.Application.Interfaces;
using F1_Bot.Domain.Constants;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Application;

public class StandingsService : IStandingsService
{
    private const int CacheExpirationMinutes = 5;

    private readonly IOpenF1Client _openF1Client;
    private readonly ICalendarService _calendarService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<StandingsService> _logger;

    public StandingsService(
        IOpenF1Client openF1Client,
        ICalendarService calendarService,
        IMemoryCache cache,
        ILogger<StandingsService> logger)
    {
        _openF1Client = openF1Client;
        _calendarService = calendarService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<List<DriverStanding>> GetDriverStandingsAsync(int? year = null, int? round = null, int? meetingKey = null)
    {
        try
        {
            var effectiveYear = year ?? DateTime.UtcNow.Year;
            int? resolvedMeetingKey = meetingKey;
            if (!resolvedMeetingKey.HasValue)
            {
                _logger.LogDebug("Getting driver standings for year {Year}, round {Round} (season end)", effectiveYear, round);
                resolvedMeetingKey = await GetMeetingKeyForYearRoundAsync(effectiveYear, round);
            }
            else
            {
                _logger.LogDebug("Getting driver standings for meeting {MeetingKey} (after specific race)", resolvedMeetingKey);
            }

            if (!resolvedMeetingKey.HasValue)
            {
                _logger.LogWarning("No meeting key found for year {Year}, round {Round}", effectiveYear, round);
                return new List<DriverStanding>();
            }

            var cacheKey = $"driver_standings_{resolvedMeetingKey.Value}";
            if (_cache.TryGetValue<List<DriverStanding>>(cacheKey, out var cachedDriverStandings))
            {
                _logger.LogDebug("Returning cached driver standings for meeting {MeetingKey}", resolvedMeetingKey);
                return cachedDriverStandings ?? new List<DriverStanding>();
            }

            var championshipTask = _openF1Client.GetDriverChampionshipByMeetingKeyAsync(resolvedMeetingKey.Value);
            var driverLookupTask = GetDriverNameLookupForMeetingAsync(resolvedMeetingKey.Value);
            await Task.WhenAll(championshipTask, driverLookupTask).ConfigureAwait(false);

            var championship = await championshipTask;
            var driverLookup = await driverLookupTask;

            if (championship.Count == 0)
            {
                _logger.LogWarning("No championship data found for meeting {MeetingKey}", resolvedMeetingKey);
                return new List<DriverStanding>();
            }

            var standings = championship
                .OrderBy(d => d.Position_Current)
                .Take(20)
                .Select(d =>
                {
                    var hasName = driverLookup.TryGetValue(d.Driver_Number, out var nameAndTeam);
                    return new DriverStanding
                    {
                        Position = d.Position_Current,
                        DriverNumber = d.Driver_Number,
                        Points = (int)d.Points_Current,
                        DriverName = hasName ? nameAndTeam.Name : string.Empty,
                        TeamName = hasName ? nameAndTeam.Team : string.Empty
                    };
                })
                .ToList();

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            _cache.Set(cacheKey, standings, cacheOptions);

            _logger.LogDebug("Successfully retrieved {Count} driver standings", standings.Count);
            return standings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting driver standings");
            return new List<DriverStanding>();
        }
    }

    public async Task<List<TeamStanding>> GetTeamStandingsAsync(int? year = null, int? round = null, int? meetingKey = null)
    {
        try
        {
            var effectiveYear = year ?? DateTime.UtcNow.Year;
            int? resolvedMeetingKey = meetingKey;
            if (!resolvedMeetingKey.HasValue)
            {
                _logger.LogDebug("Getting team standings for year {Year}, round {Round} (season end)", effectiveYear, round);
                resolvedMeetingKey = await GetMeetingKeyForYearRoundAsync(effectiveYear, round);
            }
            else
            {
                _logger.LogDebug("Getting team standings for meeting {MeetingKey} (after specific race)", resolvedMeetingKey);
            }

            if (!resolvedMeetingKey.HasValue)
            {
                _logger.LogWarning("No meeting key found for year {Year}, round {Round}", effectiveYear, round);
                return new List<TeamStanding>();
            }

            var teamCacheKey = $"team_standings_{resolvedMeetingKey.Value}";
            if (_cache.TryGetValue<List<TeamStanding>>(teamCacheKey, out var cachedTeamStandings))
            {
                _logger.LogDebug("Returning cached team standings for meeting {MeetingKey}", resolvedMeetingKey);
                return cachedTeamStandings ?? new List<TeamStanding>();
            }

            var openF1Teams = await _openF1Client.GetTeamChampionshipByMeetingKeyAsync(resolvedMeetingKey.Value);
            if (openF1Teams.Count == 0)
            {
                _logger.LogWarning("No team championship data found for meeting {MeetingKey}", resolvedMeetingKey);
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

            var teamCacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
            };
            _cache.Set(teamCacheKey, standings, teamCacheOptions);

            _logger.LogDebug("Successfully retrieved {Count} team standings", standings.Count);
            return standings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting team standings");
            return new List<TeamStanding>();
        }
    }

    private async Task<Dictionary<int, (string Name, string Team)>> GetDriverNameLookupForMeetingAsync(int meetingKey)
    {
        var lookupCacheKey = $"driver_lookup_{meetingKey}";
        if (_cache.TryGetValue<Dictionary<int, (string Name, string Team)>>(lookupCacheKey, out var cachedLookup))
        {
            _logger.LogDebug("Returning cached driver lookup for meeting {MeetingKey}", meetingKey);
            return cachedLookup ?? new Dictionary<int, (string Name, string Team)>();
        }

        var sessions = await _openF1Client.GetSessionsAsync(OpenF1SessionName.Race, meetingKey.ToString());
        var session = sessions.OrderBy(s => s.Date_Start ?? DateTime.MinValue).FirstOrDefault();
        if (session == null)
        {
            _logger.LogDebug("No Race session found for meeting {MeetingKey}, driver names will be empty", meetingKey);
            return new Dictionary<int, (string Name, string Team)>();
        }

        var drivers = await _openF1Client.GetDriversAsync(session.Session_Key.ToString());
        var lookup = drivers
            .GroupBy(d => d.Driver_Number)
            .ToDictionary(g => g.Key, g => (g.First().Full_Name ?? string.Empty, g.First().Team_Name ?? string.Empty));

        var lookupCacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(CacheExpirationMinutes)
        };
        _cache.Set(lookupCacheKey, lookup, lookupCacheOptions);

        return lookup;
    }

    private async Task<int?> GetMeetingKeyForYearRoundAsync(int year, int? round)
    {
        try
        {
            var races = await _calendarService.GetRacesAsync(year);
            if (races.Count == 0)
            {
                _logger.LogWarning("No races found for year {Year}", year);
                return null;
            }

            Race? targetRace = round.HasValue
                ? races.FirstOrDefault(r => r.RoundNumber == round.Value)
                : races.OrderByDescending(r => r.Date).FirstOrDefault();

            if (targetRace == null)
            {
                _logger.LogWarning(round.HasValue
                    ? "Round {Round} not found for year {Year}"
                    : "No races found for year {Year}", round, year);
                return null;
            }

            return targetRace.Id;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting meeting key for year {Year}, round {Round}", year, round);
            return null;
        }
    }

}
