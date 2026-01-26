using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface IRaceDetailsService
{
    Task<RaceDetails?> GetRaceByRoundAsync(int round, int? year = null);
    Task<List<RaceDetails>> GetAllRacesWithDetailsAsync(int? year = null);
}
