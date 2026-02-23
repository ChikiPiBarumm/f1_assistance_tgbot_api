using F1_Bot.Domain.Constants;
using F1_Bot.Domain.Models;
using F1_Bot.Infrastructure.OpenF1;

namespace F1_Bot.Application;

/// <summary>
/// Shared mapping from OpenF1 meeting DTOs to domain race models.
/// Used by CalendarService and RaceDetailsService so meeting → race logic lives in one place.
/// </summary>
public static class OpenF1MeetingMapper
{
    public static Race ToRace(OpenF1MeetingDto meeting, int roundNumber)
    {
        return new Race
        {
            Id = meeting.Meeting_Key,
            Name = meeting.Meeting_Name,
            CircuitName = meeting.Location,
            City = meeting.Location,
            Country = meeting.Country_Name,
            RoundNumber = roundNumber,
            Date = meeting.Date_Start,
            Status = GetStatus(meeting)
        };
    }

    public static RaceDetails ToRaceDetails(OpenF1MeetingDto meeting, int roundNumber)
    {
        return new RaceDetails
        {
            Id = meeting.Meeting_Key,
            Name = meeting.Meeting_Name,
            CircuitName = meeting.Location,
            City = meeting.Location,
            Country = meeting.Country_Name,
            RoundNumber = roundNumber,
            Date = meeting.Date_Start,
            Status = GetStatus(meeting)
        };
    }

    private static string GetStatus(OpenF1MeetingDto meeting) =>
        meeting.Date_End < DateTime.UtcNow ? RaceStatus.Completed : RaceStatus.Upcoming;
}
