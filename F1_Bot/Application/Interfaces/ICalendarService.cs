using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface ICalendarService
{
    Task<List<Race>> GetRacesAsync(int? year = null);
    Task<Race?> GetNextRaceAsync(int? year = null);
}
