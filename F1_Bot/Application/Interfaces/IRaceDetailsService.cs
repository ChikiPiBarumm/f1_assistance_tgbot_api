using F1_Bot.Domain.Models;

namespace F1_Bot.Application.Interfaces;

public interface IRaceDetailsService
{
    Task<RaceDetails?> GetRaceByRoundAsync(int round, int? year = null);
    Task<RaceDetails?> GetRaceByMeetingKeyAsync(int meetingKey, int round, int? year = null);
    Task<RaceDetails?> GetNextRaceDetailsAsync(int? year = null);
    Task<List<RaceDetails>> GetAllRacesWithDetailsAsync(int? year = null);
}
