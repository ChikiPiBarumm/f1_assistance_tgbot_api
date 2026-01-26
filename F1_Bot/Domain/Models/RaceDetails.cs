namespace F1_Bot.Domain.Models;

public class RaceDetails
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CircuitName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public int RoundNumber { get; set; }

    public DateTime Date { get; set; }

    public string Status { get; set; } = "Upcoming";

    public List<Session> Sessions { get; set; } = new();
}
