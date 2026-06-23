namespace LankaConnect.Modules.Payments.Contracts;

/// <summary>
/// Cross-module read-only projection of a Stripe-customer mapping. Returned by
/// <see cref="IPaymentQueries"/> methods that surface Stripe customer state to
/// User-aggregate consumers (e.g. account-settings detail page).
/// </summary>
/// <remarks>
/// Wave 4.4.a (2026-06-23). Most callers only need <see cref="StripeCustomerId"/>
/// and use the <see cref="IPaymentQueries.GetStripeCustomerIdAsync"/> shortcut
/// instead - this DTO is for the rarer "show customer details on the profile
/// page" path.
/// </remarks>
public sealed record StripeCustomerSummaryDto(
    Guid UserId,
    string StripeCustomerId,
    string Email,
    string? Name,
    DateTime StripeCreatedAt);
