using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.TestHelpers;

public static class ContactInformationBuilder
{
    public static ContactInformation Create()
    {
        var result = ContactInformation.Create(
            email: "test@example.com",
            phoneNumber: "+94771234567",
            website: "https://testbusiness.lk",
            facebookPage: "https://facebook.com/testbusiness",
            instagramHandle: "@testbusiness",
            twitterHandle: "@testbusiness"
        );

        return result.Value;
    }

    public static ContactInformation CreateEmailOnly()
    {
        var result = ContactInformation.Create(
            email: "test@example.com"
        );

        return result.Value;
    }

    public static ContactInformation CreatePhoneOnly()
    {
        var result = ContactInformation.Create(
            phoneNumber: "+94771234567"
        );

        return result.Value;
    }

    public static ContactInformation CreateWithWebsiteOnly()
    {
        var result = ContactInformation.Create(
            website: "https://testbusiness.lk"
        );

        return result.Value;
    }
}