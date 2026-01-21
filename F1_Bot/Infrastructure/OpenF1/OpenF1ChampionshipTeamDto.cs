namespace F1_Bot.Infrastructure.OpenF1;

// Represents one row from /v1/championship_teams (constructors standings)
public class OpenF1ChampionshipTeamDto
{
    public string Team_Name { get; set; } = string.Empty;
    public double Points_Current { get; set; }
    public int Position_Current { get; set; }

    public int Meeting_Key { get; set; }
    public int Session_Key { get; set; }
}