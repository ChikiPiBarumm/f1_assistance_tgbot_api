using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class OpenF1CalendarService : ICalendarService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OpenF1CalendarService> _logger;
    private static readonly TimeSpan CurrentSeasonCacheExpiration = TimeSpan.FromMinutes(5);
    private static readonly TimeSpan HistoricalCacheExpiration = TimeSpan.FromHours(1);
    private const int FirstF1Season = 1950;

    public OpenF1CalendarService(IOpenF1Client openF1Client, IMemoryCache cache, ILogger<OpenF1CalendarService> logger)
    {
        _openF1Client = openF1Client;
        _cache = cache;
        _logger = logger;
    }

    public static bool IsValidYear(int year)
    {
        var currentYear = DateTime.UtcNow.Year;
        return year >= FirstF1Season && year <= currentYear + 1;
    }

    public async Task<List<Race>> GetRacesAsync(int? year = null)
    {
        try
        {
            year ??= DateTime.UtcNow.Year;

            if (!IsValidYear(year.Value))
            {
                _logger.LogWarning("Invalid year {Year}. Valid range: {FirstYear}-{LastYear}", year, FirstF1Season, DateTime.UtcNow.Year + 1);
                return new List<Race>();
            }

            var cacheKey = $"race_calendar_{year}";

            if (_cache.TryGetValue<List<Race>>(cacheKey, out var cachedRaces))
            {
                _logger.LogDebug("Returning cached race calendar for year {Year}", year);
                return cachedRaces ?? new List<Race>();
            }

            _logger.LogInformation("Getting race calendar for year {Year}", year);

            var meetings = await _openF1Client.GetMeetingsAsync(year.Value);

            if (meetings.Count == 0)
            {
                _logger.LogWarning("No meetings found for year {Year}", year);
                return new List<Race>();
            }

            var races = meetings
                .OrderBy(m => m.Date_Start)
                .Select((m, index) => new Race
                {
                    Id = m.Meeting_Key,
                    Name = m.Meeting_Name,
                    CircuitName = m.Location,
                    City = m.Location,
                    Country = m.Country_Name,
                    RoundNumber = index + 1,
                    Date = m.Date_Start,
                    Status = m.Date_End < DateTime.UtcNow ? "Completed" : "Upcoming"
                })
                .ToList();

            _logger.LogInformation("Successfully mapped {Count} races from OpenF1 data", races.Count);

            var isHistorical = year < DateTime.UtcNow.Year;
            var cacheExpiration = isHistorical ? HistoricalCacheExpiration : CurrentSeasonCacheExpiration;

            var cacheOptions = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = cacheExpiration
            };
            _cache.Set(cacheKey, races, cacheOptions);

            return races;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race calendar");
            return new List<Race>();
        }
    }

    public async Task<Race?> GetNextRaceAsync(int? year = null)
    {
        try
        {
            _logger.LogDebug("Getting next upcoming race for year {Year}", year ?? DateTime.UtcNow.Year);

            var races = await GetRacesAsync(year);

            var nextRace = races
                .Where(r => r.Status == "Upcoming")
                .OrderBy(r => r.Date)
                .FirstOrDefault();

            if (nextRace == null)
            {
                _logger.LogWarning("No upcoming race found");
            }
            else
            {
                _logger.LogDebug("Next race: {RaceName} on {Date}", nextRace.Name, nextRace.Date);
            }

            return nextRace;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting next race");
            return null;
        }
    }
}
