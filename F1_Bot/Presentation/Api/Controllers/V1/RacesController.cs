using F1_Bot.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1_Bot.Presentation.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class RacesController : ControllerBase
{
    private readonly ICalendarService _calendarService;
    private readonly IStandingsService _standingsService;
    private readonly IRaceResultsService _raceResultsService;

    public RacesController(
        ICalendarService calendarService,
        IStandingsService standingsService,
        IRaceResultsService raceResultsService)
    {
        _calendarService = calendarService;
        _standingsService = standingsService;
        _raceResultsService = raceResultsService;
    }

    [HttpGet]
    public async Task<ActionResult> GetRaces()
    {
        var races = await _calendarService.GetRacesAsync();
        return Ok(races);
    }

    [HttpGet("next")]
    public async Task<ActionResult> GetNextRace()
    {
        var nextRace = await _calendarService.GetNextRaceAsync();

        if (nextRace is null)
        {
            return NotFound(new { message = "No upcoming race found" });
        }

        return Ok(nextRace);
    }

    [HttpGet("last/results")]
    public async Task<ActionResult> GetLastRaceResults()
    {
        var results = await _raceResultsService.GetLastRaceResultsAsync();

        if (results.Count == 0)
        {
            return NotFound(new { message = "No race results found for the latest race" });
        }

        return Ok(results);
    }
}
