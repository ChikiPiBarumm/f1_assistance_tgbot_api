using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

// Interface: This is a "contract" that says "any class implementing this
// must have a method called GetRaces() that returns a list of Race objects"
public interface ICalendarService
{
    // Returns all races in the calendar
    Task<List<Race>> GetRacesAsync();

    // Returns the next upcoming race
    Task<Race?> GetNextRaceAsync();
}