using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Services;

public class OpenF1CalendarService : ICalendarService
{
    private readonly IOpenF1Client _openF1Client;
    private readonly ILogger<OpenF1CalendarService> _logger;

    public OpenF1CalendarService(IOpenF1Client openF1Client, ILogger<OpenF1CalendarService> logger)
    {
        _openF1Client = openF1Client;
        _logger = logger;
    }

    public async Task<List<Race>> GetRacesAsync()
    {
        try
        {
            var year = DateTime.UtcNow.Year;
            _logger.LogInformation("Getting race calendar for year {Year}", year);

            var meetings = await _openF1Client.GetMeetingsAsync(year);

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
            return races;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while getting race calendar");
            return new List<Race>();
        }
    }

    public async Task<Race?> GetNextRaceAsync()
    {
        try
        {
            _logger.LogDebug("Getting next upcoming race");

            var races = await GetRacesAsync();

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