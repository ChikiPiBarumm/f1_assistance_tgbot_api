namespace F1_Bot.Infrastructure.OpenF1;

// Represents one row from /v1/drivers
public class OpenF1DriverDto
{
    public int Driver_Number { get; set; }

    // Example: "Max Verstappen"
    public string Full_Name { get; set; } = string.Empty;

    // Example: "VER"
    public string Name_Acronym { get; set; } = string.Empty;

    // Example: "Red Bull Racing"
    public string Team_Name { get; set; } = string.Empty;

    // Might be useful later
    public int? Meeting_Key { get; set; }
    public int? Session_Key { get; set; }
    public int? Year { get; set; }
}