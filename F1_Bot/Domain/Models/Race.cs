namespace F1_Bot.Domain.Models;

public class Race
{
    // Unique ID for this race in your system.
    // In practice, this might map to an OpenF1 "meeting_key".
    public int Id { get; set; }

    // Example: "Bahrain Grand Prix"
    public string Name { get; set; } = string.Empty;

    // Example: "Bahrain International Circuit"
    public string CircuitName { get; set; } = string.Empty;

    // Example: "Sakhir"
    public string City { get; set; } = string.Empty;

    // Example: "Bahrain"
    public string Country { get; set; } = string.Empty;

    // Which round of the season this race is (1, 2, 3, ...)
    public int RoundNumber { get; set; }

    // Date of the main race.
    public DateTime Date { get; set; }

    // Simple status flag for v1: "Upcoming" or "Completed"
    public string Status { get; set; } = "Upcoming";
}