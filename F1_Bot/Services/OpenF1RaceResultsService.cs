using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;

namespace F1_Bot.Services;

public class OpenF1RaceResultsService : IRaceResultsService
{
    private readonly IOpenF1Client _openF1Client;

    public OpenF1RaceResultsService(IOpenF1Client openF1Client)
    {
        _openF1Client = openF1Client;
    }

    public async Task<List<RaceResult>> GetLastRaceResultsAsync()
    {
        // Step 1: Get the latest race session
        const string sessionType = "Race";
        const string meetingKey = "latest";

        var sessions = await _openF1Client.GetSessionsAsync(sessionType, meetingKey);

        // Find the most recent race session (or just take the first one if only one)
        var latestRaceSession = sessions
            .OrderByDescending(s => s.Date_Start ?? DateTime.MinValue)
            .FirstOrDefault();

        if (latestRaceSession == null)
        {
            // No race session found
            return new List<RaceResult>();
        }

        // Step 2: Get results for that session
        var sessionKey = latestRaceSession.Session_Key.ToString();
        var results = await _openF1Client.GetSessionResultsAsync(sessionKey);

        // Map OpenF1SessionResultDto -> RaceResult
        var mapped = results
            .OrderBy(r => r.Position)
            .Select(r => new RaceResult
            {
                Position = r.Position,
                DriverName = r.Full_Name,
                DriverNumber = r.Driver_Number,
                TeamName = r.Team_Name,
                Points = (int)r.Points,
                Status = string.IsNullOrWhiteSpace(r.Status) ? "Finished" : r.Status
            })
            .ToList();

        return mapped;
    }
}