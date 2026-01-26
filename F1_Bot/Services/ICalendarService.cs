using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface ICalendarService
{
    Task<List<Race>> GetRacesAsync();
    Task<Race?> GetNextRaceAsync();
}