namespace LankaConnect.Domain.Events.Enums;

public enum EventStatus
{
    Draft = 0,
    Published = 1,
    Active = 2,
    Postponed = 3,
    Cancelled = 4,
    Completed = 5,
    Archived = 6,
    UnderReview = 7,
    // Phase 8YA.1: TBD-dates lifecycle. Events created without start/end dates start
    // in Planning; SetDates(...) transitions Planning → Draft once both dates are set.
    // Q1=A allows publishing a Planning event directly (Planning → Published also valid).
    Planning = 8
}
