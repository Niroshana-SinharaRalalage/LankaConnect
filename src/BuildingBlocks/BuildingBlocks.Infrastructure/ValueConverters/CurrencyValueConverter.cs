using LankaConnect.SharedKernel.Money;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LankaConnect.BuildingBlocks.Infrastructure.ValueConverters;

/// <summary>
/// Consult #23 (2026-07-10) — EF Core value converter that persists <see cref="Currency"/>
/// as its 3-letter ISO 4217 code (e.g. <c>"USD"</c>) and rehydrates via
/// <see cref="Currency.FromCode"/>.
///
/// <para>
/// Registered globally per DbContext via <c>ConfigureConventions</c>:
/// <code>
/// protected override void ConfigureConventions(ModelConfigurationBuilder cb)
/// {
///     cb.Properties&lt;Currency&gt;().HasConversion&lt;CurrencyValueConverter&gt;();
/// }
/// </code>
/// </para>
///
/// <para>
/// <b>Why this exists</b>: EF Core 8's <c>OwnsOne&lt;Money&gt;()</c> discovery walks Money's
/// public ctor <c>Money(decimal amount, Currency currency)</c>. The <c>currency</c> parameter's
/// type is a VO (not scalar), so EF fails ctor binding with
/// <c>"No suitable constructor was found for entity type 'X.Y#Money'. Cannot bind 'amount',
/// 'currency'."</c>. Registering Currency as scalar-convertible makes the ctor bindable and
/// unblocks every OwnsOne&lt;Money&gt; site uniformly (TicketPrice, Pricing.AdultPrice,
/// Pricing.ChildPrice, GroupPricingTier.PricePerPerson, RevenueBreakdown, RefundRequest amounts,
/// Sponsor amounts, Donation amounts, Collection amounts, AddOnPurchase amounts).
/// </para>
///
/// <para>
/// <b>Round-trip safety</b>: Currency instances are singletons from <see cref="Currency"/>.All
/// registry; equality by <see cref="Currency.Code"/> guarantees FromCode(Code) returns the
/// same instance. GetEqualityComponents yields Code so ValueObject equality is preserved.
/// </para>
/// </summary>
public sealed class CurrencyValueConverter : ValueConverter<Currency, string>
{
    public CurrencyValueConverter()
        : base(
            currency => currency.Code,
            code => Currency.FromCode(code))
    {
    }
}
