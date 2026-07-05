using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using System.Text.Json;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Converters;

/// <summary>
/// EF Core value converter for nullable <see cref="Money"/> value objects, backed
/// by a JSON string column. Mirrors the legacy
/// <c>LankaConnect.SPLIT_PER_ENTITY.Converters.MoneyConverter</c> — Wave 6.5.e
/// (2026-07-03) relocated the LankaEvents EF configurations into
/// <c>Products.LankaEvents.Infrastructure</c>; keeping the converter alongside
/// them avoids re-taking a transitional dependency on the legacy Data namespace
/// (which would have required a <c>[Wave6_5TransitionalException]</c> baseline
/// expansion — architect-forbidden until Wave 6.5.f). The legacy copy remains
/// referenced by non-Event-family configurations (e.g., Business, StripeCustomer)
/// that still live under <c>LankaConnect.Infrastructure</c>. When those are
/// carved out in a future wave, the legacy pair can be deleted.
/// </summary>
public class MoneyConverter : ValueConverter<Money?, string?>
{
    public MoneyConverter() : base(
        money => money == null ? null : SerializeMoney(money),
        json => json == null ? null : DeserializeMoney(json))
    {
    }

    private static string SerializeMoney(Money money)
    {
        var data = new
        {
            Amount = money.Amount,
            Currency = money.Currency.ToString()
        };
        return JsonSerializer.Serialize(data);
    }

    private static Money? DeserializeMoney(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<MoneyData>(json);
            if (data == null) return null;

            if (Enum.TryParse<Currency>(data.Currency, out var currency))
            {
                return Money.Create(data.Amount, currency).Value;
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    private class MoneyData
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
