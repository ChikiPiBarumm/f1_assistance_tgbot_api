namespace F1_Bot.Domain.Models;

public class Session
{
    public string SessionType { get; set; } = string.Empty;
    
    public string SessionName { get; set; } = string.Empty;
    
    public DateTime? StartTime { get; set; }
    
    public DateTime? EndTime { get; set; }
}
