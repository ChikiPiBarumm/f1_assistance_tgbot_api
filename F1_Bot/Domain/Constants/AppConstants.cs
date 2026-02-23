namespace F1_Bot.Domain.Constants;

/// <summary>Race status values used in domain models and services.</summary>
public static class RaceStatus
{
    public const string Upcoming = "Upcoming";
    public const string Completed = "Completed";
}

/// <summary>Session name values for the OpenF1 API (session_name parameter).</summary>
public static class OpenF1SessionName
{
    public const string Race = "Race";
}

/// <summary>Season bounds and validation (year range, max rounds).</summary>
public static class SeasonConstants
{
    public const int FirstF1Season = 2023;
    public const int MaxRoundsPerSeason = 24;

    public static bool IsValidYear(int year)
    {
        var currentYear = DateTime.UtcNow.Year;
        return year >= FirstF1Season && year <= currentYear + 1;
    }
}
