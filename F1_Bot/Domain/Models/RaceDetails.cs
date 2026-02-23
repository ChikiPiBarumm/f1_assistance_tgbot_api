using F1_Bot.Domain.Constants;

namespace F1_Bot.Domain.Models;

/// <summary>
/// Race metadata (round, circuit, date, status).
/// For session schedule (FP1, Qualifying, Race times), use <see cref="F1_Bot.Application.Interfaces.ISessionService"/>.
/// </summary>
public class RaceDetails
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string CircuitName { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string Country { get; set; } = string.Empty;

    public int RoundNumber { get; set; }

    public DateTime Date { get; set; }

    public string Status { get; set; } = RaceStatus.Upcoming;
}
