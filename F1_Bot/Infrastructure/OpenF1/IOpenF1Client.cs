namespace F1_Bot.Infrastructure.OpenF1;

public interface IOpenF1Client
{
    Task<List<OpenF1MeetingDto>> GetMeetingsAsync(int year, CancellationToken cancellationToken = default);

    Task<List<OpenF1ChampionshipDriverDto>> GetDriverChampionshipAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    Task<List<OpenF1ChampionshipTeamDto>> GetTeamChampionshipAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    Task<List<OpenF1DriverDto>> GetDriversAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);

    // New: get sessions (e.g., to find latest race session)
    Task<List<OpenF1SessionDto>> GetSessionsAsync(
        string sessionType,
        string meetingKey,
        CancellationToken cancellationToken = default);

    // Updated: now just needs session_key (we'll get it from sessions first)
    Task<List<OpenF1SessionResultDto>> GetSessionResultsAsync(
        string sessionKey,
        CancellationToken cancellationToken = default);
}