using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users.Enums;

namespace LankaConnect.Domain.Users.ValueObjects;

/// <summary>
/// Value object representing an external social login linked to a user account
/// Immutable - once created, cannot be modified (only added/removed from User's collection)
/// </summary>
public class ExternalLogin : ValueObject
{
    /// <summary>
    /// The federated social provider (Facebook, Google, Apple, Microsoft)
    /// </summary>
    public FederatedProvider Provider { get; private set; }

    /// <summary>
    /// The external user ID from the provider (stored as Entra OID)
    /// This is the 'oid' claim from the Entra token
    /// </summary>
    public string ExternalProviderId { get; private set; }

    /// <summary>
    /// The email address associated with this external login
    /// May differ from the user's primary email in LankaConnect
    /// </summary>
    public string ProviderEmail { get; private set; }

    /// <summary>
    /// When this external login was linked to the user's account
    /// </summary>
    public DateTime LinkedAt { get; private set; }

    // EF Core constructor
    private ExternalLogin()
    {
        ExternalProviderId = null!;
        ProviderEmail = null!;
    }

    private ExternalLogin(
        FederatedProvider provider,
        string externalProviderId,
        string providerEmail,
        DateTime linkedAt)
    {
        Provider = provider;
        ExternalProviderId = externalProviderId;
        ProviderEmail = providerEmail;
        LinkedAt = linkedAt;
    }

    /// <summary>
    /// Creates a new ExternalLogin value object
    /// </summary>
    /// <param name="provider">The federated social provider</param>
    /// <param name="externalProviderId">The external user ID from the provider (Entra OID)</param>
    /// <param name="providerEmail">The email from the provider</param>
    /// <returns>Result containing the ExternalLogin or an error message</returns>
    public static Result<ExternalLogin> Create(
        FederatedProvider provider,
        string? externalProviderId,
        string? providerEmail)
    {
        if (string.IsNullOrWhiteSpace(externalProviderId))
            return Result<ExternalLogin>.Failure("External provider ID is required");

        if (string.IsNullOrWhiteSpace(providerEmail))
            return Result<ExternalLogin>.Failure("Provider email is required");

        var login = new ExternalLogin(
            provider,
            externalProviderId.Trim(),
            providerEmail.Trim(),
            DateTime.UtcNow);

        return Result<ExternalLogin>.Success(login);
    }

    /// <summary>
    /// For EF Core: Create ExternalLogin with specific LinkedAt timestamp (used when loading from database)
    /// Internal to prevent external misuse
    /// </summary>
    internal static ExternalLogin CreateInternal(
        FederatedProvider provider,
        string externalProviderId,
        string providerEmail,
        DateTime linkedAt)
    {
        return new ExternalLogin(provider, externalProviderId, providerEmail, linkedAt);
    }

    /// <summary>
    /// Value objects are equal if Provider and ExternalProviderId match
    /// LinkedAt and ProviderEmail are not part of equality (can change over time)
    /// </summary>
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Provider;
        yield return ExternalProviderId;
    }

    public override string ToString()
    {
        return $"{Provider.ToDisplayName()}: {ExternalProviderId}";
    }
}
