using F1_Bot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace F1_Bot.Presentation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RacesController : ControllerBase
{
    private readonly IRaceDetailsService _raceDetailsService;
    private readonly ISessionService _sessionService;
    private readonly IRaceResultsService _raceResultsService;

    public RacesController(
        IRaceDetailsService raceDetailsService,
        ISessionService sessionService,
        IRaceResultsService raceResultsService)
    {
        _raceDetailsService = raceDetailsService;
        _sessionService = sessionService;
        _raceResultsService = raceResultsService;
    }

    [HttpGet]
    public async Task<ActionResult> GetRaces()
    {
        var races = await _raceDetailsService.GetAllRacesWithDetailsAsync();
        return Ok(races);
    }

    [HttpGet("{round}")]
    public async Task<ActionResult> GetRaceByRound(int round)
    {
        if (round < 1)
        {
            return BadRequest(new { message = "Round number must be greater than 0" });
        }

        var race = await _raceDetailsService.GetRaceByRoundAsync(round);

        if (race is null)
        {
            return NotFound(new { message = $"Race not found for round {round}" });
        }

        return Ok(race);
    }

    [HttpGet("{round}/sessions")]
    public async Task<ActionResult> GetRaceSessions(int round)
    {
        if (round < 1)
        {
            return BadRequest(new { message = "Round number must be greater than 0" });
        }

        var schedule = await _sessionService.GetRaceScheduleAsync(round);

        if (schedule is null)
        {
            return NotFound(new { message = $"Race schedule not found for round {round}" });
        }

        return Ok(schedule);
    }

    [HttpGet("{roundOrLatest}/results")]
    public async Task<ActionResult> GetRaceResults(string roundOrLatest)
    {
        if (string.Equals(roundOrLatest, "latest", StringComparison.OrdinalIgnoreCase))
        {
            var results = await _raceResultsService.GetLastRaceResultsAsync();
            if (results.Count == 0)
            {
                return NotFound(new { message = "No race results found for the latest race" });
            }
            return Ok(results);
        }

        if (!int.TryParse(roundOrLatest, out var round) || round < 1)
        {
            return BadRequest(new { message = "Use a round number (1 or greater) or 'latest' for the most recent race results" });
        }

        var resultsByRound = await _raceResultsService.GetRaceResultsByRoundAsync(round);

        if (resultsByRound.Count == 0)
        {
            return NotFound(new { message = $"No race results found for round {round}" });
        }

        return Ok(resultsByRound);
    }

    [HttpGet("next")]
    public async Task<ActionResult> GetNextRace()
    {
        var raceDetails = await _raceDetailsService.GetNextRaceDetailsAsync();

        if (raceDetails is null)
        {
            return NotFound(new { message = "No upcoming race found" });
        }

        return Ok(raceDetails);
    }
}
