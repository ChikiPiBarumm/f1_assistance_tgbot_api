using System.Linq;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class OpenF1RaceDetailsService : IRaceDetailsService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ISessionService _sessionService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenF1RaceDetailsService> _logger;
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(5);

    public OpenF1RaceDetailsService(
        IOpenF1Client openF1Client,
        ISessionService sessionService,
        IMemoryCache cache,
        ILogger<OpenF1RaceDetailsService> logger)
    {
        _openF1Client = openF1Client;
        _sessionService = sessionService;
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

            var meeting = orderedMeetings[round - 1];
            var schedule = await _sessionService.GetRaceScheduleByMeetingKeyAsync(meeting.Meeting_Key);

            var raceDetails = new RaceDetails
            {
                Id = meeting.Meeting_Key,
                Name = meeting.Meeting_Name,
                CircuitName = meeting.Location,
                City = meeting.Location,
                Country = meeting.Country_Name,
                RoundNumber = round,
                Date = meeting.Date_Start,
                Status = meeting.Date_End < DateTime.UtcNow ? "Completed" : "Upcoming",
                Sessions = schedule?.Sessions ?? new List<Session>()
            };

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
                var meeting = orderedMeetings[i];
                var schedule = await _sessionService.GetRaceScheduleByMeetingKeyAsync(meeting.Meeting_Key);

                racesDetails.Add(new RaceDetails
                {
                    Id = meeting.Meeting_Key,
                    Name = meeting.Meeting_Name,
                    CircuitName = meeting.Location,
                    City = meeting.Location,
                    Country = meeting.Country_Name,
                    RoundNumber = i + 1,
                    Date = meeting.Date_Start,
                    Status = meeting.Date_End < DateTime.UtcNow ? "Completed" : "Upcoming",
                    Sessions = schedule?.Sessions ?? new List<Session>()
                });
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
