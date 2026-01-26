using F1_Bot.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1_Bot.Presentation.Api.Controllers.V2;

[ApiController]
[Route("api/v2/[controller]")]
public class RacesController : ControllerBase
{
    private readonly IRaceDetailsService _raceDetailsService;
    private readonly ISessionService _sessionService;
    private readonly IRaceResultsService _raceResultsService;
    private readonly ICalendarService _calendarService;

    public RacesController(
        IRaceDetailsService raceDetailsService,
        ISessionService sessionService,
        IRaceResultsService raceResultsService,
        ICalendarService calendarService)
    {
        _raceDetailsService = raceDetailsService;
        _sessionService = sessionService;
        _raceResultsService = raceResultsService;
        _calendarService = calendarService;
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

    [HttpGet("{round}/results")]
    public async Task<ActionResult> GetRaceResults(int round)
    {
        if (round < 1)
        {
            return BadRequest(new { message = "Round number must be greater than 0" });
        }

        var results = await _raceResultsService.GetRaceResultsByRoundAsync(round);

        if (results.Count == 0)
        {
            return NotFound(new { message = $"No race results found for round {round}" });
        }

        return Ok(results);
    }

    [HttpGet("next")]
    public async Task<ActionResult> GetNextRace()
    {
        var nextRace = await _calendarService.GetNextRaceAsync();

        if (nextRace is null)
        {
            return NotFound(new { message = "No upcoming race found" });
        }

        var raceDetails = await _raceDetailsService.GetRaceByRoundAsync(nextRace.RoundNumber);
        return Ok(raceDetails);
    }
}
