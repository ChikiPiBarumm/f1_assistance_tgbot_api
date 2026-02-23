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
        var url = $"/v1/meetings?year={year}";
        var result = await GetJsonWithRetryAsync<List<OpenF1MeetingDto>>(url, $"meetings for year {year}", cancellationToken);
        if (result != null)
            _logger.LogInformation("Successfully fetched {Count} meetings from OpenF1", result.Count);
        return result ?? new List<OpenF1MeetingDto>();
    }

    public async Task<OpenF1MeetingDto?> GetMeetingByKeyAsync(int meetingKey, CancellationToken cancellationToken = default)
    {
        var url = $"/v1/meetings?meeting_key={meetingKey}";
        var result = await GetJsonWithRetryAsync<List<OpenF1MeetingDto>>(url, $"meeting {meetingKey}", cancellationToken);
        return result?.FirstOrDefault();
    }

    private async Task<T?> GetJsonWithRetryAsync<T>(string url, string errorContext, CancellationToken cancellationToken) where T : class
    {
        try
        {
            _logger.LogInformation("[OpenF1] GET {Endpoint}", url);
            return await GetWith429RetryAsync<T>(url, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while fetching OpenF1 data: {Context}", errorContext);
            return null;
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "Request timeout while fetching OpenF1 data: {Context}", errorContext);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while fetching OpenF1 data: {Context}", errorContext);
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

    public async Task<List<OpenF1ChampionshipDriverDto>> GetDriverChampionshipByMeetingKeyAsync(
        int meetingKey,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/championship_drivers?meeting_key={meetingKey}";
        var result = await GetJsonWithRetryAsync<List<OpenF1ChampionshipDriverDto>>(url, $"driver championship for meeting {meetingKey}", cancellationToken);
        return result ?? new List<OpenF1ChampionshipDriverDto>();
    }

    public async Task<List<OpenF1ChampionshipTeamDto>> GetTeamChampionshipByMeetingKeyAsync(
        int meetingKey,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/championship_teams?meeting_key={meetingKey}";
        var result = await GetJsonWithRetryAsync<List<OpenF1ChampionshipTeamDto>>(url, $"team championship for meeting {meetingKey}", cancellationToken);
        return result ?? new List<OpenF1ChampionshipTeamDto>();
    }

    public async Task<List<OpenF1DriverDto>> GetDriversAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/drivers?session_key={sessionKey}";
        var result = await GetJsonWithRetryAsync<List<OpenF1DriverDto>>(url, $"drivers for session {sessionKey}", cancellationToken);
        return result ?? new List<OpenF1DriverDto>();
    }

    public async Task<List<OpenF1SessionDto>> GetSessionsAsync(
        string sessionName,
        string meetingKey,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/sessions?session_name={sessionName}&meeting_key={meetingKey}";
        var result = await GetJsonWithRetryAsync<List<OpenF1SessionDto>>(url, $"sessions name={sessionName} meeting={meetingKey}", cancellationToken);
        return result ?? new List<OpenF1SessionDto>();
    }

    public async Task<List<OpenF1SessionResultDto>> GetSessionResultsAsync(
        string sessionKey,
        CancellationToken cancellationToken = default)
    {
        var url = $"/v1/session_result?session_key={sessionKey}";
        var result = await GetJsonWithRetryAsync<List<OpenF1SessionResultDto>>(url, $"session results for session {sessionKey}", cancellationToken);
        return result ?? new List<OpenF1SessionResultDto>();
    }
}