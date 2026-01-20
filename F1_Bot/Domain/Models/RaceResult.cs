namespace F1_Bot.Domain.Models;

public class RaceResult
{
    // The race this result belongs to
    public int RaceId { get; set; }

    // Position in the final classification (1 = winner)
    public int Position { get; set; }

    // Example: "Max Verstappen"
    public string DriverName { get; set; } = string.Empty;

    // Example: 1, 44, 16, ...
    public int DriverNumber { get; set; }

    // Example: "Red Bull Racing"
    public string TeamName { get; set; } = string.Empty;

    // Championship points scored in this race
    public int Points { get; set; }

    // Example: "Finished", "DNF", "DSQ"
    public string Status { get; set; } = "Finished";
}