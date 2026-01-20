using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

// This is a "fake" service that returns hard-coded data.
// Later, we'll replace this with a real service that calls OpenF1.
public class FakeCalendarService : ICalendarService
{
    // Hard-coded list of races for testing
    private readonly List<Race> _races = new()
    {
        new Race
        {
            Id = 1,
            Name = "Bahrain Grand Prix",
            CircuitName = "Bahrain International Circuit",
            City = "Sakhir",
            Country = "Bahrain",
            RoundNumber = 1,
            Date = new DateTime(2025, 3, 2),
            Status = "Completed"
        },
        new Race
        {
            Id = 2,
            Name = "Saudi Arabian Grand Prix",
            CircuitName = "Jeddah Corniche Circuit",
            City = "Jeddah",
            Country = "Saudi Arabia",
            RoundNumber = 2,
            Date = new DateTime(2025, 3, 9),
            Status = "Completed"
        },
        new Race
        {
            Id = 3,
            Name = "Australian Grand Prix",
            CircuitName = "Albert Park Circuit",
            City = "Melbourne",
            Country = "Australia",
            RoundNumber = 3,
            Date = new DateTime(2025, 3, 23),
            Status = "Upcoming"
        },
        new Race
        {
            Id = 4,
            Name = "Japanese Grand Prix",
            CircuitName = "Suzuka International Racing Course",
            City = "Suzuka",
            Country = "Japan",
            RoundNumber = 4,
            Date = new DateTime(2025, 4, 13),
            Status = "Upcoming"
        }
    };

    // Returns all races
    public Task<List<Race>> GetRacesAsync()
    {
        // Task.FromResult wraps a synchronous result in a Task
        // This is fine for now since we're just returning in-memory data
        return Task.FromResult(_races);
    }

    // Returns the next upcoming race (first race with Status = "Upcoming")
    public Task<Race?> GetNextRaceAsync()
    {
        var nextRace = _races
            .Where(r => r.Status == "Upcoming")
            .OrderBy(r => r.Date)
            .FirstOrDefault();

        return Task.FromResult(nextRace);
    }
}