namespace F1_Bot.Domain.Models;

public class DriverStanding
{
    // Standing position in the championship
    public int Position { get; set; }

    // Example: "Lando Norris"
    public string DriverName { get; set; } = string.Empty;

    // Example: 4, 81, ...
    public int DriverNumber { get; set; }

    // Example: "McLaren"
    public string TeamName { get; set; } = string.Empty;

    // Total points in the championship
    public int Points { get; set; }
}