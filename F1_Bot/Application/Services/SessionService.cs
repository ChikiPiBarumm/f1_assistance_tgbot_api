using System.Linq;
using F1_Bot.Application.Interfaces;
using F1_Bot.Domain.Constants;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Application;

public class SessionService : ISessionService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ICalendarService _calendarService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<SessionService> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public SessionService(
        IOpenF1Client openF1Client,
        ICalendarService calendarService,
        IMemoryCache cache,
        ILogger<SessionService> logger)
    {
        _openF1Client = openF1Client;
        _calendarService = calendarService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RaceSchedule?> GetRaceScheduleAsync(int round, int? year = null)
    {
        try
        {
            var races = await _calendarService.GetRacesAsync(year);
            var race = races.FirstOrDefault(r => r.RoundNumber == round);

            if (race == null)
            {
                _logger.LogWarning("Race not found for round {Round}", round);
                return null;
            }

            return await GetRaceScheduleByMeetingKeyAsync(race.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race schedule for round {Round}", round);
            return null;
        }
    }

    public async Task<RaceSchedule?> GetRaceScheduleByMeetingKeyAsync(int meetingKey)
    {
        try
        {
            var cacheKey = $"race_schedule_{meetingKey}";

            if (_cache.TryGetValue<RaceSchedule>(cacheKey, out var cachedSchedule))
            {
                _logger.LogDebug("Returning cached race schedule for meeting {MeetingKey}", meetingKey);
                return cachedSchedule;
            }

            _logger.LogDebug("Getting race schedule for meeting {MeetingKey}", meetingKey);

            var allSessions = await _openF1Client.GetSessionsAsync("", meetingKey.ToString());

            if (allSessions.Count == 0)
            {
                _logger.LogWarning("No sessions found for meeting {MeetingKey}", meetingKey);
                return null;
            }

            var races = await _calendarService.GetRacesAsync();
            var race = races.FirstOrDefault(r => r.Id == meetingKey);

            var sessions = allSessions
                .OrderBy(s => s.Date_Start ?? DateTime.MinValue)
                .Select(s => new Session
                {
                    SessionType = s.Session_Type,
                    SessionName = s.Session_Name,
                    StartTime = s.Date_Start,
                    EndTime = s.Date_End
                })
                .ToList();

            var schedule = new RaceSchedule
            {
                RaceId = meetingKey,
                RaceName = race?.Name ?? "Unknown Race",
                Sessions = sessions
            };

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration
            };
            _cache.Set(cacheKey, schedule, cacheOptions);

            return schedule;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race schedule for meeting {MeetingKey}", meetingKey);
            return null;
        }
    }

    public async Task<string?> GetRaceSessionKeyAsync(int meetingKey, int? round = null)
    {
        try
        {
            var cacheKey = $"race_session_key_{meetingKey}_{(round.HasValue ? round.Value.ToString() : "latest")}";
            if (_cache.TryGetValue<string>(cacheKey, out var cachedSessionKey))
            {
                _logger.LogDebug("Returning cached race session key for meeting {MeetingKey}", meetingKey);
                return cachedSessionKey;
            }

            _logger.LogDebug("Getting race session key for meeting {MeetingKey}, round {Round}", meetingKey, round);

            var sessions = await _openF1Client.GetSessionsAsync(OpenF1SessionName.Race, meetingKey.ToString());

            if (sessions.Count == 0)
            {
                _logger.LogWarning("No race sessions found for meeting {MeetingKey}", meetingKey);
                return null;
            }

            var raceSession = round.HasValue
                ? sessions.FirstOrDefault(s => s.Meeting_Key == meetingKey)
                : sessions.OrderByDescending(s => s.Date_Start ?? DateTime.MinValue).FirstOrDefault();

            if (raceSession == null)
            {
                _logger.LogWarning("No race session found for meeting {MeetingKey}", meetingKey);
                return null;
            }

            var sessionKey = raceSession.Session_Key.ToString();
            _cache.Set(cacheKey, sessionKey, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheExpiration });
            return sessionKey;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race session key for meeting {MeetingKey}", meetingKey);
            return null;
        }
    }
}
