using F1_Bot.Domain.Models;

namespace F1_Bot.Application.Interfaces;

public interface IRaceResultsService
{
    Task<List<RaceResult>> GetLastRaceResultsAsync(int? year = null);
    Task<(int MeetingKey, int Round, int Year)?> GetLastRaceMeetingInfoAsync();
    Task<List<RaceResult>> GetRaceResultsByRoundAsync(int round, int? year = null);
    Task<List<RaceResult>> GetRaceResultsByMeetingKeyAsync(int meetingKey);
}
