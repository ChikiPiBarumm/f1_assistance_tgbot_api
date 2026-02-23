using F1_Bot.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace F1_Bot.Presentation.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StandingsController : ControllerBase
{
    private readonly IStandingsService _standingsService;

    public StandingsController(IStandingsService standingsService)
    {
        _standingsService = standingsService;
    }

    [HttpGet("drivers")]
    public async Task<ActionResult> GetDriverStandings()
    {
        var standings = await _standingsService.GetDriverStandingsAsync();
        return Ok(standings);
    }

    [HttpGet("teams")]
    public async Task<ActionResult> GetTeamStandings()
    {
        var standings = await _standingsService.GetTeamStandingsAsync();
        return Ok(standings);
    }
}
