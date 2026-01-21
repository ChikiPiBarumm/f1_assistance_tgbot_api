namespace F1_Bot.Infrastructure.OpenF1;

// Represents one row from /v1/sessions
public class OpenF1SessionDto
{
    public int Session_Key { get; set; }
    
    public int Meeting_Key { get; set; }
    
    public string Session_Name { get; set; } = string.Empty;
    
    public string Session_Type { get; set; } = string.Empty;
    
    public DateTime? Date_Start { get; set; }
    
    public DateTime? Date_End { get; set; }
    
}