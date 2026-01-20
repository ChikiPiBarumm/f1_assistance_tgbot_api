namespace F1_Bot.Domain.Models;

public class TeamStanding
{
    // Standing position in the constructors' championship
    public int Position { get; set; }

    // Example: "Ferrari"
    public string TeamName { get; set; } = string.Empty;

    // Total points in the constructors' standings
    public int Points { get; set; }
}