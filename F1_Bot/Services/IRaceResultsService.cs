using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

public interface IRaceResultsService
{
    Task<List<RaceResult>> GetLastRaceResultsAsync();
}