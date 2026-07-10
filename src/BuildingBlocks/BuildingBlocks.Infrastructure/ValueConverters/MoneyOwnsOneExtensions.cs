using LankaConnect.SharedKernel.Money;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LankaConnect.BuildingBlocks.Infrastructure.ValueConverters;

/// <summary>
/// Consult #23 (2026-07-10) — extension methods that expose the standard OwnsOne&lt;Money&gt;
/// property configuration in one call, so every configuration site can use:
/// <code>
/// builder.OwnsOne(e =&gt; e.Price, MoneyOwnsOneExtensions.ConfigureMoney);
/// </code>
/// or inline nested:
/// <code>
/// pricing.OwnsOne(p =&gt; p.AdultPrice, m =&gt; m.ConfigureMoneyProperties());
/// </code>
/// </summary>
public static class MoneyOwnsOneExtensions
{
    /// <summary>
    /// Configures the required <see cref="Money.Amount"/> and
    /// <see cref="Money.Currency"/> property mappings so EF Core 8 can bind the
    /// <c>Money(decimal amount, Currency currency)</c> ctor. Currency is
    /// persisted via <see cref="CurrencyValueConverter"/>.
    /// </summary>
    public static OwnedNavigationBuilder<TOwner, Money> ConfigureMoneyProperties<TOwner>(
        this OwnedNavigationBuilder<TOwner, Money> builder)
        where TOwner : class
    {
        builder.Property(m => m.Amount);
        builder.Property(m => m.Currency).HasConversion<CurrencyValueConverter>();
        return builder;
    }
}
