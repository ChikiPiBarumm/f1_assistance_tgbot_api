namespace F1_Bot.Infrastructure.OpenF1;

// DTO = Data Transfer Object
// This represents ONE item from the OpenF1 /v1/meetings endpoint.
public class OpenF1MeetingDto
{
    // Unique ID for the meeting (race weekend)
    public int Meeting_Key { get; set; }

    // Example: "Bahrain Grand Prix"
    public string Meeting_Name { get; set; } = string.Empty;

    // Example: "Bahrain"
    public string Country_Name { get; set; } = string.Empty;

    // Example: "Sakhir"
    public string Location { get; set; } = string.Empty;

    // Example: "2023-03-03T00:00:00+00:00"
    public DateTime Date_Start { get; set; }

    // Example: "2023-03-05T00:00:00+00:00"
    public DateTime Date_End { get; set; }

    public int Year { get; set; }
}