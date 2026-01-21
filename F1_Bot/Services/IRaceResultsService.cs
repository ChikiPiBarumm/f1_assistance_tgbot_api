using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

// Service for race results
public interface IRaceResultsService
{
    // Get results for the latest completed race
    Task<List<RaceResult>> GetLastRaceResultsAsync();
}