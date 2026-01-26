namespace F1_Bot.Domain.Models;

public class RaceSchedule
{
    public int RaceId { get; set; }
    
    public string RaceName { get; set; } = string.Empty;
    
    public List<Session> Sessions { get; set; } = new();
}
