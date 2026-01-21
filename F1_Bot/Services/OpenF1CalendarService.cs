using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;

namespace F1_Bot.Services;

// Calendar service that uses OpenF1 data
public class OpenF1CalendarService : ICalendarService
{
    private readonly IOpenF1Client _openF1Client;

    public OpenF1CalendarService(IOpenF1Client openF1Client)
    {
        _openF1Client = openF1Client;
    }

    public async Task<List<Race>> GetRacesAsync()
    {
        // For now we hard-code the year.
        // Later we can make this configurable (e.g. from appsettings).
        var year = DateTime.UtcNow.Year;

        var meetings = await _openF1Client.GetMeetingsAsync(year);

        // Map OpenF1MeetingDto -> Race
        var races = meetings
            .OrderBy(m => m.Date_Start)
            .Select((m, index) => new Race
            {
                Id = m.Meeting_Key,
                Name = m.Meeting_Name,
                CircuitName = m.Location, // We don't have circuit name directly yet
                City = m.Location,
                Country = m.Country_Name,
                RoundNumber = index + 1,
                Date = m.Date_Start,
                Status = m.Date_End < DateTime.UtcNow ? "Completed" : "Upcoming"
            })
            .ToList();

        return races;
    }

    public async Task<Race?> GetNextRaceAsync()
    {
        var races = await GetRacesAsync();

        // Find the first upcoming race by date
        var nextRace = races
            .Where(r => r.Status == "Upcoming")
            .OrderBy(r => r.Date)
            .FirstOrDefault();

        return nextRace;
    }
}