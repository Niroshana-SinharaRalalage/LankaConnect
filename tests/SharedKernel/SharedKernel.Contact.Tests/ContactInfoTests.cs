using LankaConnect.SharedKernel.Geo;

namespace LankaConnect.SharedKernel.Contact.Tests;

/// <summary>
/// Unit tests for <see cref="ContactInfo"/> — the composite (Phone + Email +
/// Website + Address) VO. Wave 8.5 GAP-6 (2026-07-19).
/// </summary>
public sealed class ContactInfoTests
{
    private static Email EmailFrom(string s) => Email.Create(s).Value;
    private static PhoneNumber PhoneFrom(string s) => PhoneNumber.Create(s).Value;
    private static Address AddressAt(string city) => new("1 Main St", city, "ON", "M5V 3A8", "Canada");

    [Fact]
    public void Create_AllChannelsPopulated_Succeeds()
    {
        var phone = PhoneFrom("+1-416-555-0100");
        var email = EmailFrom("hello@example.com");

        var result = ContactInfo.Create(phone, email, "https://example.com", AddressAt("Toronto"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Phone.Should().Be(phone);
        result.Value.Email.Should().Be(email);
        result.Value.Website.Should().Be("https://example.com");
        result.Value.PhysicalAddress.Should().NotBeNull();
    }

    [Fact]
    public void Create_OnlyEmail_Succeeds()
    {
        // A listing with only email is legit — the VO accepts partial contact cards.
        var result = ContactInfo.Create(phone: null, EmailFrom("hello@example.com"), website: null, physicalAddress: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email!.Value.Should().Be("hello@example.com");
        result.Value.Phone.Should().BeNull();
    }

    [Fact]
    public void Create_OnlyPhone_Succeeds()
    {
        var result = ContactInfo.Create(PhoneFrom("+94 77 123 4567"), email: null, website: null, physicalAddress: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Phone.Should().NotBeNull();
    }

    [Fact]
    public void Create_AllChannelsNullOrEmpty_Fails()
    {
        // A "no contact at all" listing is meaningless — rejected at construction
        // so downstream aggregates never have to null-check the aggregate itself.
        var result = ContactInfo.Create(phone: (PhoneNumber?)null, email: (Email?)null, website: null, physicalAddress: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("At least one contact channel", "error message stability matters for API mapping");
    }

    [Fact]
    public void Create_AllChannelsWhitespaceOnly_Fails()
    {
        var result = ContactInfo.Create(phone: (string?)null, email: (string?)null, website: "   ", physicalAddress: null);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Create_StringOverload_ParsesPhoneAndEmail()
    {
        var result = ContactInfo.Create("+1-416-555-0100", "hello@example.com", "https://example.com", AddressAt("Toronto"));

        result.IsSuccess.Should().BeTrue();
        result.Value.Phone!.Value.Should().Be("+1-416-555-0100");
        result.Value.Email!.Value.Should().Be("hello@example.com");
    }

    [Fact]
    public void Create_StringOverload_InvalidEmail_Fails()
    {
        var result = ContactInfo.Create(phone: null, email: "not-an-email", website: null, physicalAddress: null);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("@");
    }

    [Fact]
    public void Create_TrimsWebsiteWhitespace()
    {
        var result = ContactInfo.Create(phone: null, email: EmailFrom("a@b.com"), website: "  https://example.com  ", physicalAddress: null);

        result.IsSuccess.Should().BeTrue();
        result.Value.Website.Should().Be("https://example.com");
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = ContactInfo.Create(PhoneFrom("+1-416-555-0100"), EmailFrom("hello@example.com"), "https://example.com", AddressAt("Toronto")).Value;
        var b = ContactInfo.Create(PhoneFrom("+1-416-555-0100"), EmailFrom("hello@example.com"), "https://example.com", AddressAt("Toronto")).Value;

        a.Should().Be(b);
        a.GetHashCode().Should().Be(b.GetHashCode());
    }

    [Fact]
    public void Equality_DifferentEmail_AreNotEqual()
    {
        var a = ContactInfo.Create(email: EmailFrom("a@example.com"), phone: null, website: null, physicalAddress: null).Value;
        var b = ContactInfo.Create(email: EmailFrom("b@example.com"), phone: null, website: null, physicalAddress: null).Value;

        a.Should().NotBe(b);
    }

    [Fact]
    public void Equality_TwoNullPhones_AreEqual()
    {
        // Null-safe equality: two null Phone fields must not cause reference inequality.
        var a = ContactInfo.Create(email: EmailFrom("a@example.com"), phone: null, website: null, physicalAddress: null).Value;
        var b = ContactInfo.Create(email: EmailFrom("a@example.com"), phone: null, website: null, physicalAddress: null).Value;

        a.Should().Be(b);
    }

    [Fact]
    public void ToString_IncludesPopulatedFieldsOnly()
    {
        var ci = ContactInfo.Create(phone: null, email: EmailFrom("a@example.com"), website: null, physicalAddress: null).Value;

        var s = ci.ToString();

        s.Should().Contain("Email=a@example.com");
        s.Should().NotContain("Phone=");
        s.Should().NotContain("Address=");
    }
}
