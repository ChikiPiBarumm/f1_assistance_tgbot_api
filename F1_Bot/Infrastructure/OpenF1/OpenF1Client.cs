using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Infrastructure.OpenF1;

public class OpenF1Client : IOpenF1Client
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OpenF1Client> _logger;

    public OpenF1Client(HttpClient httpClient, ILogger<OpenF1Client> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<List<OpenF1MeetingDto>> GetMeetingsAsync(int year, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/meetings?year={year}";
            _logger.LogInformation("Fetching meetings for year {Year} from OpenF1", year);
            
            var result = await _httpClient.GetFromJsonAsync<List<OpenF1MeetingDto>>(url, cancellationToken);
            
            _logger.LogInformation("Successfully fetched {Count} meetings from OpenF1", result?.Count ?? 0);
            return result ?? new List<OpenF1MeetingDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching meetings for year {Year}: {Message}", year, ex.Message);
            return new List<OpenF1MeetingDto>();
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout while fetching meetings for year {Year}", year);
            return new List<OpenF1MeetingDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching meetings for year {Year}", year);
            return new List<OpenF1MeetingDto>();
        }
    }

    public async Task<List<OpenF1ChampionshipDriverDto>> GetDriverChampionshipAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/championship_drivers?session_key={sessionKey}";
            _logger.LogDebug("Fetching driver championship for session {SessionKey}", sessionKey);
            
            var result = await _httpClient.GetFromJsonAsync<List<OpenF1ChampionshipDriverDto>>(url, cancellationToken);
            return result ?? new List<OpenF1ChampionshipDriverDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching driver championship for session {SessionKey}", sessionKey);
            return new List<OpenF1ChampionshipDriverDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching driver championship for session {SessionKey}", sessionKey);
            return new List<OpenF1ChampionshipDriverDto>();
        }
    }

    public async Task<List<OpenF1ChampionshipTeamDto>> GetTeamChampionshipAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/championship_teams?session_key={sessionKey}";
            _logger.LogDebug("Fetching team championship for session {SessionKey}", sessionKey);
            
            var result = await _httpClient.GetFromJsonAsync<List<OpenF1ChampionshipTeamDto>>(url, cancellationToken);
            return result ?? new List<OpenF1ChampionshipTeamDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching team championship for session {SessionKey}", sessionKey);
            return new List<OpenF1ChampionshipTeamDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching team championship for session {SessionKey}", sessionKey);
            return new List<OpenF1ChampionshipTeamDto>();
        }
    }
    
    public async Task<List<OpenF1DriverDto>> GetDriversAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/drivers?session_key={sessionKey}";
            _logger.LogDebug("Fetching drivers for session {SessionKey}", sessionKey);
            
            var result = await _httpClient.GetFromJsonAsync<List<OpenF1DriverDto>>(url, cancellationToken);
            return result ?? new List<OpenF1DriverDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching drivers for session {SessionKey}", sessionKey);
            return new List<OpenF1DriverDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching drivers for session {SessionKey}", sessionKey);
            return new List<OpenF1DriverDto>();
        }
    }

    public async Task<List<OpenF1SessionDto>> GetSessionsAsync(
        string sessionType,
        string meetingKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/sessions?session_type={sessionType}&meeting_key={meetingKey}";
            _logger.LogDebug("Fetching sessions: type={SessionType}, meeting={MeetingKey}", sessionType, meetingKey);
            
            var result = await _httpClient.GetFromJsonAsync<List<OpenF1SessionDto>>(url, cancellationToken);
            return result ?? new List<OpenF1SessionDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching sessions: type={SessionType}, meeting={MeetingKey}", sessionType, meetingKey);
            return new List<OpenF1SessionDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching sessions: type={SessionType}, meeting={MeetingKey}", sessionType, meetingKey);
            return new List<OpenF1SessionDto>();
        }
    }

    public async Task<List<OpenF1SessionResultDto>> GetSessionResultsAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/session_result?session_key={sessionKey}";
            _logger.LogDebug("Fetching session results for session {SessionKey}", sessionKey);
            
            var result = await _httpClient.GetFromJsonAsync<List<OpenF1SessionResultDto>>(url, cancellationToken);
            return result ?? new List<OpenF1SessionResultDto>();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching session results for session {SessionKey}", sessionKey);
            return new List<OpenF1SessionResultDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while fetching session results for session {SessionKey}", sessionKey);
            return new List<OpenF1SessionResultDto>();
        }
    }
}