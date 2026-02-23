using System.Linq;
using F1_Bot.Application.Interfaces;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Application;

public class RaceDetailsService : IRaceDetailsService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ICalendarService _calendarService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<RaceDetailsService> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public RaceDetailsService(
        IOpenF1Client openF1Client,
        ICalendarService calendarService,
        IMemoryCache cache,
        ILogger<RaceDetailsService> logger)
    {
        _openF1Client = openF1Client;
        _calendarService = calendarService;
        _cache = cache;
        _logger = logger;
    }

    public async Task<RaceDetails?> GetRaceByRoundAsync(int round, int? year = null)
    {
        try
        {
            year ??= DateTime.UtcNow.Year;
            var cacheKey = $"race_details_{year}_{round}";

            if (_cache.TryGetValue<RaceDetails>(cacheKey, out var cachedRace))
            {
                _logger.LogDebug("Returning cached race details for round {Round}", round);
                return cachedRace;
            }

            _logger.LogInformation("Getting race details for round {Round}", round);

            var meetings = await _openF1Client.GetMeetingsAsync(year.Value);
            var orderedMeetings = meetings.OrderBy(m => m.Date_Start).ToList();

            if (round < 1 || round > orderedMeetings.Count)
            {
                _logger.LogWarning("Invalid round number {Round}. Total races: {Total}", round, orderedMeetings.Count);
                return null;
            }

            var meetingKey = orderedMeetings[round - 1].Meeting_Key;
            var meeting = await _openF1Client.GetMeetingByKeyAsync(meetingKey);

            if (meeting == null)
            {
                _logger.LogWarning("Meeting not found for key {MeetingKey}", meetingKey);
                return null;
            }

            var raceDetails = OpenF1MeetingMapper.ToRaceDetails(meeting, round);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration
            };
            _cache.Set(cacheKey, raceDetails, cacheOptions);

            return raceDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race details for round {Round}", round);
            return null;
        }
    }

    public async Task<RaceDetails?> GetRaceByMeetingKeyAsync(int meetingKey, int round, int? year = null)
    {
        try
        {
            year ??= DateTime.UtcNow.Year;
            var cacheKey = $"race_details_meeting_{meetingKey}";

            if (_cache.TryGetValue<RaceDetails>(cacheKey, out var cachedRace))
            {
                _logger.LogDebug("Returning cached race details for meeting {MeetingKey}", meetingKey);
                return cachedRace;
            }

            _logger.LogInformation("Getting race details for meeting {MeetingKey}", meetingKey);

            var meeting = await _openF1Client.GetMeetingByKeyAsync(meetingKey);

            if (meeting == null)
            {
                _logger.LogWarning("Meeting not found for key {MeetingKey}", meetingKey);
                return null;
            }

            var raceDetails = OpenF1MeetingMapper.ToRaceDetails(meeting, round);

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration
            };
            _cache.Set(cacheKey, raceDetails, cacheOptions);

            return raceDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race details for meeting {MeetingKey}", meetingKey);
            return null;
        }
    }

    public async Task<RaceDetails?> GetNextRaceDetailsAsync(int? year = null)
    {
        var nextRace = await _calendarService.GetNextRaceAsync(year);
        if (nextRace == null)
            return null;
        return await GetRaceByRoundAsync(nextRace.RoundNumber, nextRace.Date.Year);
    }

    public async Task<List<RaceDetails>> GetAllRacesWithDetailsAsync(int? year = null)
    {
        try
        {
            year ??= DateTime.UtcNow.Year;
            var cacheKey = $"all_races_details_{year}";

            if (_cache.TryGetValue<List<RaceDetails>>(cacheKey, out var cachedRaces))
            {
                _logger.LogDebug("Returning cached all races details for year {Year}", year);
                return cachedRaces ?? new List<RaceDetails>();
            }

            _logger.LogInformation("Getting all races with details for year {Year}", year);

            var meetings = await _openF1Client.GetMeetingsAsync(year.Value);
            var orderedMeetings = meetings.OrderBy(m => m.Date_Start).ToList();

            var racesDetails = new List<RaceDetails>();

            for (int i = 0; i < orderedMeetings.Count; i++)
            {
                racesDetails.Add(OpenF1MeetingMapper.ToRaceDetails(orderedMeetings[i], i + 1));
            }

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheExpiration
            };
            _cache.Set(cacheKey, racesDetails, cacheOptions);

            return racesDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting all races with details");
            return new List<RaceDetails>();
        }
    }
}
