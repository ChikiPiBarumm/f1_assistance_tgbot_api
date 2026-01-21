using System.Net.Http;
using System.Net.Http.Json;

namespace F1_Bot.Infrastructure.OpenF1;

public class OpenF1Client : IOpenF1Client
{
    private readonly HttpClient _httpClient;

    public OpenF1Client(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<OpenF1MeetingDto>> GetMeetingsAsync(int year, CancellationToken cancellationToken = default)
    {
        var url = $"/v1/meetings?year={year}";
        var result = await _httpClient.GetFromJsonAsync<List<OpenF1MeetingDto>>(url, cancellationToken);
        return result ?? new List<OpenF1MeetingDto>();
    }

    public async Task<List<OpenF1ChampionshipDriverDto>> GetDriverChampionshipAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/championship_drivers?session_key={sessionKey}";
        var result = await _httpClient.GetFromJsonAsync<List<OpenF1ChampionshipDriverDto>>(url, cancellationToken);
        return result ?? new List<OpenF1ChampionshipDriverDto>();
    }

    public async Task<List<OpenF1ChampionshipTeamDto>> GetTeamChampionshipAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/championship_teams?session_key={sessionKey}";
        var result = await _httpClient.GetFromJsonAsync<List<OpenF1ChampionshipTeamDto>>(url, cancellationToken);
        return result ?? new List<OpenF1ChampionshipTeamDto>();
    }
    
    public async Task<List<OpenF1DriverDto>> GetDriversAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        // Use the same session_key as for championship ("latest")
        var url = $"/v1/drivers?session_key={sessionKey}";
        var result = await _httpClient.GetFromJsonAsync<List<OpenF1DriverDto>>(url, cancellationToken);
        return result ?? new List<OpenF1DriverDto>();
    }

    public async Task<List<OpenF1SessionDto>> GetSessionsAsync(
        string sessionType,
        string meetingKey,
        CancellationToken cancellationToken = default)
    {
        // Example: /v1/sessions?session_name=Race
        var url = $"/v1/sessions?session_type={sessionType}&meeting_key={meetingKey}";
        var result = await _httpClient.GetFromJsonAsync<List<OpenF1SessionDto>>(url, cancellationToken);
        return result ?? new List<OpenF1SessionDto>();
    }

    public async Task<List<OpenF1SessionResultDto>> GetSessionResultsAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        // Example: /v1/session_result?session_key=9159
        var url = $"/v1/session_result?session_key={sessionKey}";
        var result = await _httpClient.GetFromJsonAsync<List<OpenF1SessionResultDto>>(url, cancellationToken);
        return result ?? new List<OpenF1SessionResultDto>();
    }
}