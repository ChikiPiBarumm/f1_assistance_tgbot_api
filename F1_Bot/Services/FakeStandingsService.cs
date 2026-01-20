using F1_Bot.Domain.Models;

namespace F1_Bot.Services;

// Fake service that returns hard-coded standings data
// Later, we'll replace this with a real service that calls OpenF1
public class FakeStandingsService : IStandingsService
{
    // Hard-coded driver standings (top 10 for example)
    private readonly List<DriverStanding> _driverStandings = new()
    {
        new DriverStanding
        {
            Position = 1,
            DriverName = "Max Verstappen",
            DriverNumber = 1,
            TeamName = "Red Bull Racing",
            Points = 423
        },
        new DriverStanding
        {
            Position = 2,
            DriverName = "Lando Norris",
            DriverNumber = 4,
            TeamName = "McLaren",
            Points = 410
        },
        new DriverStanding
        {
            Position = 3,
            DriverName = "Charles Leclerc",
            DriverNumber = 16,
            TeamName = "Ferrari",
            Points = 395
        },
        new DriverStanding
        {
            Position = 4,
            DriverName = "Oscar Piastri",
            DriverNumber = 81,
            TeamName = "McLaren",
            Points = 380
        },
        new DriverStanding
        {
            Position = 5,
            DriverName = "Carlos Sainz",
            DriverNumber = 55,
            TeamName = "Ferrari",
            Points = 365
        },
        new DriverStanding
        {
            Position = 6,
            DriverName = "Lewis Hamilton",
            DriverNumber = 44,
            TeamName = "Mercedes",
            Points = 340
        },
        new DriverStanding
        {
            Position = 7,
            DriverName = "George Russell",
            DriverNumber = 63,
            TeamName = "Mercedes",
            Points = 320
        },
        new DriverStanding
        {
            Position = 8,
            DriverName = "Fernando Alonso",
            DriverNumber = 14,
            TeamName = "Aston Martin",
            Points = 295
        },
        new DriverStanding
        {
            Position = 9,
            DriverName = "Sergio Pérez",
            DriverNumber = 11,
            TeamName = "Red Bull Racing",
            Points = 280
        },
        new DriverStanding
        {
            Position = 10,
            DriverName = "Lance Stroll",
            DriverNumber = 18,
            TeamName = "Aston Martin",
            Points = 265
        }
    };

    // Hard-coded team standings
    private readonly List<TeamStanding> _teamStandings = new()
    {
        new TeamStanding
        {
            Position = 1,
            TeamName = "Red Bull Racing",
            Points = 703
        },
        new TeamStanding
        {
            Position = 2,
            TeamName = "McLaren",
            Points = 790
        },
        new TeamStanding
        {
            Position = 3,
            TeamName = "Ferrari",
            Points = 760
        },
        new TeamStanding
        {
            Position = 4,
            TeamName = "Mercedes",
            Points = 660
        },
        new TeamStanding
        {
            Position = 5,
            TeamName = "Aston Martin",
            Points = 560
        },
        new TeamStanding
        {
            Position = 6,
            TeamName = "Alpine",
            Points = 450
        },
        new TeamStanding
        {
            Position = 7,
            TeamName = "Williams",
            Points = 380
        },
        new TeamStanding
        {
            Position = 8,
            TeamName = "Haas",
            Points = 320
        },
        new TeamStanding
        {
            Position = 9,
            TeamName = "RB",
            Points = 280
        },
        new TeamStanding
        {
            Position = 10,
            TeamName = "Sauber",
            Points = 240
        }
    };

    // Returns driver standings
    public Task<List<DriverStanding>> GetDriverStandingsAsync()
    {
        return Task.FromResult(_driverStandings);
    }

    // Returns team standings
    public Task<List<TeamStanding>> GetTeamStandingsAsync()
    {
        return Task.FromResult(_teamStandings);
    }
}