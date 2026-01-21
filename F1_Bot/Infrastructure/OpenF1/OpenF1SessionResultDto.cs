namespace F1_Bot.Infrastructure.OpenF1;

// Represents one row from a race session result endpoint
public class OpenF1SessionResultDto
{
    public int Driver_Number { get; set; }

    // Example: "Max Verstappen"
    public string Full_Name { get; set; } = string.Empty;

    // Example: "Red Bull Racing"
    public string Team_Name { get; set; } = string.Empty;

    // Final classification position (1 = winner)
    public int Position { get; set; }

    // Championship points scored in this race
    public double Points { get; set; }

    // Example: "Finished", "DNF"
    public string Status { get; set; } = string.Empty;

    public int Meeting_Key { get; set; }
    public int Session_Key { get; set; }
}