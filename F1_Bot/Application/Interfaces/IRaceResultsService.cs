using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface IRaceResultsService
{
    Task<List<RaceResult>> GetLastRaceResultsAsync(int? year = null);
    Task<(List<RaceResult> Results, int MeetingKey, string? RaceName, int? Year)> GetLastRaceResultsWithMeetingKeyAsync();
    Task<(int MeetingKey, int Round, int Year)?> GetLastRaceMeetingInfoAsync();
    Task<List<RaceResult>> GetRaceResultsByRoundAsync(int round, int? year = null);
    Task<List<RaceResult>> GetRaceResultsByMeetingKeyAsync(int meetingKey);
}
