using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.Enums;
using LankaConnect.Domain.Users.ValueObjects;
using UserEmail = LankaConnect.Domain.Users.ValueObjects.Email;

namespace LankaConnect.Domain.Tests.TestHelpers;

public static class TestDataFactory
{
    public static LankaConnect.Domain.Business.Business ValidBusiness()
    {
        return LankaConnect.Domain.Business.Business.Create(
            ValidBusinessProfile(),
            ValidBusinessLocation(),
            ValidContactInformation(),
            ValidBusinessHours(),
            BusinessCategory.Restaurant,
            Guid.NewGuid()).Value;
    }

    public static BusinessProfile ValidBusinessProfile()
    {
        return BusinessProfileBuilder.Create();
    }

    public static BusinessLocation ValidBusinessLocation()
    {
        return BusinessLocationBuilder.Create();
    }

    public static ContactInformation ValidContactInformation()
    {
        return ContactInformationBuilder.Create();
    }

    public static BusinessHours ValidBusinessHours()
    {
        var hours = new Dictionary<DayOfWeek, (TimeOnly? open, TimeOnly? close)>
        {
            [DayOfWeek.Monday] = (new TimeOnly(9, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Tuesday] = (new TimeOnly(9, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Wednesday] = (new TimeOnly(9, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Thursday] = (new TimeOnly(9, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Friday] = (new TimeOnly(9, 0), new TimeOnly(17, 0)),
            [DayOfWeek.Saturday] = (null, null), // Closed
            [DayOfWeek.Sunday] = (null, null)    // Closed
        };

        return BusinessHours.Create(hours).Value;
    }

    public static UserEmail ValidEmail(string? emailAddress = null)
    {
        return UserEmail.Create(emailAddress ?? "test@lankaconnect.com").Value;
    }
}