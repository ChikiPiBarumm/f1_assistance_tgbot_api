using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace F1_Bot.Infrastructure.OpenF1;

public class OpenF1Client : IOpenF1Client
{
    private const int DefaultRetryDelayMs = 2000;
    private const int MaxRetriesOn429 = 2;

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
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);

            var result = await GetWith429RetryAsync<List<OpenF1MeetingDto>>(url, cancellationToken);

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

    public async Task<OpenF1MeetingDto?> GetMeetingByKeyAsync(int meetingKey, CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/meetings?meeting_key={meetingKey}";
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);

            var result = await GetWith429RetryAsync<List<OpenF1MeetingDto>>(url, cancellationToken);
            return result?.FirstOrDefault();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching meeting {MeetingKey}: {Message}", meetingKey, ex.Message);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout while fetching meeting {MeetingKey}", meetingKey);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching meeting {MeetingKey}", meetingKey);
            return null;
        }
    }

    private async Task<T?> GetWith429RetryAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        for (var attempt = 1; attempt <= MaxRetriesOn429; attempt++)
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);

            if (response.StatusCode == (HttpStatusCode)429)
            {
                if (attempt == MaxRetriesOn429)
                {
                    _logger.LogWarning("OpenF1 returned 429 (Too Many Requests) for {Url} after {Attempt} attempt(s)", url, attempt);
                    return null;
                }

                var delayMs = DefaultRetryDelayMs;
                if (response.Headers.RetryAfter?.Delta is { } delta)
                    delayMs = (int)Math.Min(delta.TotalMilliseconds, 10_000);
                else if (response.Headers.RetryAfter?.Date is { } retryDate)
                    delayMs = Math.Max(0, (int)(retryDate - DateTimeOffset.UtcNow).TotalMilliseconds);

                _logger.LogInformation("OpenF1 429 for {Url}, retrying in {DelayMs}ms (attempt {Attempt})", url, delayMs, attempt);
                await Task.Delay(delayMs, cancellationToken);
                continue;
            }

            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
        }

        return null;
    }

    public async Task<List<OpenF1ChampionshipDriverDto>> GetDriverChampionshipAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var url = $"/v1/championship_drivers?session_key={sessionKey}";
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);
            
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
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);
            
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
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);
            
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
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);

            var result = await GetWith429RetryAsync<List<OpenF1SessionDto>>(url, cancellationToken);
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
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);

            var result = await GetWith429RetryAsync<List<OpenF1SessionResultDto>>(url, cancellationToken);
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