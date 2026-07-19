using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.SharedKernel.Geo;

namespace LankaConnect.SharedKernel.Contact;

/// <summary>
/// Composite contact-info value object. Bundles the standard "how do I reach this
/// entity" quadruple — <see cref="Phone"/>, <see cref="Email"/>, <see cref="Website"/>,
/// <see cref="PhysicalAddress"/> — into a single VO consumed by directory-listing
/// aggregates (Business profile, Home listing, Mart seller, Seyla service provider,
/// Nivasa resource entry).
/// </summary>
/// <remarks>
/// <para>
/// Authored 2026-07-19 as part of Wave 8.5 GAP-6 (Geo capability cluster) per
/// <c>docs/architecture/COMMON_COMPONENTS_INVENTORY_2026_07_16.md</c> §4.2-4.5.
/// Placed in <c>SharedKernel.Contact</c> alongside <see cref="Email"/> +
/// <see cref="PhoneNumber"/> — the natural home for a composite contact primitive.
/// The <see cref="PhysicalAddress"/> field is intentionally an
/// <c>SharedKernel.Geo.Address</c> so listings can flow into geo-radius queries
/// without a second lookup.
/// </para>
/// <para>
/// All four fields are optional. A directory listing may publish an email only,
/// a phone only, or a full contact card — the VO does not force cardinality.
/// The <c>Create</c> factory returns <c>Failure</c> only if EVERY field is null/empty:
/// a "no contact channel at all" listing is meaningless and rejected at construction
/// so downstream aggregates never have to null-check the aggregate itself.
/// </para>
/// <para>
/// Equality follows <see cref="ValueObject"/> conventions: two ContactInfo instances
/// are equal iff all four component equalities match. Null fields compare equal.
/// </para>
/// </remarks>
public sealed class ContactInfo : ValueObject
{
    /// <summary>Phone value object, or null if no phone channel published.</summary>
    public PhoneNumber? Phone { get; }

    /// <summary>Email value object, or null if no email channel published.</summary>
    public Email? Email { get; }

    /// <summary>Website URL (unstructured string — validation deferred; free-form to accommodate multi-domain listings).</summary>
    public string? Website { get; }

    /// <summary>Postal address value object, or null if no physical location.</summary>
    public Address? PhysicalAddress { get; }

    // EF parameterless constructor (matches Email + PhoneNumber pattern).
    private ContactInfo()
    {
        Phone = null;
        Email = null;
        Website = null;
        PhysicalAddress = null;
    }

    private ContactInfo(PhoneNumber? phone, Email? email, string? website, Address? physicalAddress)
    {
        Phone = phone;
        Email = email;
        Website = string.IsNullOrWhiteSpace(website) ? null : website.Trim();
        PhysicalAddress = physicalAddress;
    }

    /// <summary>
    /// Factory. Fails only if all four channels are unpopulated — a listing with
    /// zero contact channels is rejected at construction.
    /// </summary>
    public static Result<ContactInfo> Create(
        PhoneNumber? phone,
        Email? email,
        string? website,
        Address? physicalAddress)
    {
        var websiteTrimmed = string.IsNullOrWhiteSpace(website) ? null : website.Trim();

        var allEmpty = phone is null
                       && email is null
                       && string.IsNullOrEmpty(websiteTrimmed)
                       && physicalAddress is null;

        if (allEmpty)
            return Result<ContactInfo>.Failure("At least one contact channel (phone, email, website, or address) is required.");

        return Result<ContactInfo>.Success(new ContactInfo(phone, email, websiteTrimmed, physicalAddress));
    }

    /// <summary>
    /// Overload that accepts raw strings — parses Phone + Email via their factory
    /// gates. Any parse failure short-circuits with the underlying error message.
    /// Useful for API-layer callers that receive DTO strings and want a single
    /// validation entry point.
    /// </summary>
    public static Result<ContactInfo> Create(string? phone, string? email, string? website, Address? physicalAddress)
    {
        PhoneNumber? phoneVo = null;
        if (!string.IsNullOrWhiteSpace(phone))
        {
            var phoneResult = PhoneNumber.Create(phone);
            if (phoneResult.IsFailure) return Result<ContactInfo>.Failure(phoneResult.Error);
            phoneVo = phoneResult.Value;
        }

        Email? emailVo = null;
        if (!string.IsNullOrWhiteSpace(email))
        {
            var emailResult = Email.Create(email);
            if (emailResult.IsFailure) return Result<ContactInfo>.Failure(emailResult.Error);
            emailVo = emailResult.Value;
        }

        return Create(phoneVo, emailVo, website, physicalAddress);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        // Yield null-safe placeholders so ValueObject.Equals treats two nulls as equal.
        yield return Phone as object ?? "__nophone__";
        yield return Email as object ?? "__noemail__";
        yield return Website ?? "__nowebsite__";
        yield return PhysicalAddress as object ?? "__noaddress__";
    }

    public override string ToString()
    {
        var parts = new List<string>();
        if (Phone is not null) parts.Add($"Phone={Phone}");
        if (Email is not null) parts.Add($"Email={Email}");
        if (!string.IsNullOrEmpty(Website)) parts.Add($"Website={Website}");
        if (PhysicalAddress is not null) parts.Add($"Address={PhysicalAddress}");
        return parts.Count == 0 ? "ContactInfo(empty)" : string.Join("; ", parts);
    }
}
