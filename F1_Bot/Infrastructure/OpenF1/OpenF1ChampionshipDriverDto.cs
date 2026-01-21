namespace F1_Bot.Infrastructure.OpenF1;

// Represents one row from /v1/championship_drivers
public class OpenF1ChampionshipDriverDto
{
    public int Driver_Number { get; set; }
    public double Points_Current { get; set; }
    public int Position_Current { get; set; }

    // Might be useful later
    public int Meeting_Key { get; set; }
    public int Session_Key { get; set; }
}