using F1_Bot.Services;
using Microsoft.AspNetCore.Mvc;

namespace F1_Bot.Presentation.Api.Controllers.V1;

[ApiController]
[Route("api/v1/[controller]")]
public class RacesController : ControllerBase
{
    private readonly ICalendarService _calendarService;

    public RacesController(ICalendarService calendarService)
    {
        _calendarService = calendarService;
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

}
