using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Application.Common;
using LankaConnect.SharedKernel.Money;

namespace LankaConnect.Products.LankaEvents.Application.Common;

// Day 4 slot C sub-slice 4C.d.iv (2026-07-06, Consult #13 Q2 pattern):
// Transitional caller-side shim. LankaEvents.Application inherited ~90 legacy
// `MoneyBuilder.Create(a, c)` sites from the pre-SharedKernel-Money.Money era. The
// SharedKernel.Money type exposes only `new Money(decimal, Currency)` per
// architect ruling (no legacy Create/Add/Multiply aliases in SharedKernel).
//
// Rewriting each site to `new Money(...)` + inlining the Result<T> unwrap
// is Day 5 slot B (Wave 6.5.f LankaEvents handler migration) territory.
// This shim keeps compile-green during the intermediate cutover; caller
// signatures stay identical (`Result<Money>`).
//
// Delete when LankaEvents handlers rewrite to operator form.
public static class MoneyBuilder
{
    public static Result<Money> Create(decimal amount, Currency? currency)
        => currency is null
            ? Result<Money>.Failure("Currency is required")
            : Result<Money>.Success(new Money(amount, currency));

    /// <summary>
    /// Replacement for `Enum.TryParse&lt;Currency&gt;(code, true, out var currency)` which
    /// no longer works because SharedKernel.Money.Currency is a class, not an enum.
    /// Wraps Currency.TryFromCode into a Try-pattern signature.
    /// </summary>
    public static bool TryParseCurrency(string? code, out Currency? currency)
    {
        var m = Currency.TryFromCode(code);
        if (m.HasValue)
        {
            currency = m.Value;
            return true;
        }
        currency = null;
        return false;
    }
}
