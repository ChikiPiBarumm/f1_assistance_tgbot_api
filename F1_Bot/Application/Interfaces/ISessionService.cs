using F1_Bot.Domain.Models;

namespace F1_Bot.Application.Interfaces;

public interface ISessionService
{
    Task<RaceSchedule?> GetRaceScheduleAsync(int round, int? year = null);
    Task<RaceSchedule?> GetRaceScheduleByMeetingKeyAsync(int meetingKey);
    Task<string?> GetRaceSessionKeyAsync(int meetingKey, int? round = null);
}
