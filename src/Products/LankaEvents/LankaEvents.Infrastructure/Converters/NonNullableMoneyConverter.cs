using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LankaConnect.BuildingBlocks.Domain.Shared.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Shared.Enums;
using System.Text.Json;
namespace LankaConnect.Products.LankaEvents.Infrastructure.Converters;

/// <summary>
/// EF Core value converter for non-nullable <see cref="Money"/> value objects
/// (e.g. <c>TicketTier.AdultPrice</c>). Mirrors the legacy
/// <c>LankaConnect.SPLIT_PER_ENTITY.Converters.NonNullableMoneyConverter</c>
/// under this product's own namespace so the moved EF configurations do not
/// have to take a transitional dependency on the legacy Data namespace. See
/// the <see cref="MoneyConverter"/> XML doc for the Wave 6.5.e rationale.
/// </summary>
/// <remarks>
/// 2026-06-11 HOTFIX carried over from the legacy converter: <c>DeserializeMoney</c>
/// wraps its JSON parse in try/catch. Without this, a single corrupt row (e.g.
/// <c>adult_price</c> with embedded 0x00 bytes) would propagate a
/// <c>System.Text.Json.JsonReaderException</c> up through EF Core and 500 the
/// entire <c>GetAllAsync</c> query — breaking dashboard endpoints for ALL users
/// until the bad row was found and fixed. The fallback returns
/// <c>Money(0, USD)</c> matching the original "shouldn't happen for required
/// columns" semantics, but emits a structured <c>Console.Error</c> warning
/// identifying the raw value so an operator can grep container logs for the
/// offending row.
/// </remarks>
public class NonNullableMoneyConverter : ValueConverter<Money, string>
{
    public NonNullableMoneyConverter() : base(
        money => SerializeMoney(money),
        json => DeserializeMoney(json))
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

    private static Money DeserializeMoney(string json)
    {
        try
        {
            var data = JsonSerializer.Deserialize<MoneyData>(json);
            if (data != null && Enum.TryParse<Currency>(data.Currency, out var currency))
            {
                return Money.Create(data.Amount, currency).Value;
            }
        }
        catch (System.Exception ex)
        {
            var sanitized = Sanitize(json);
            System.Console.Error.WriteLine(
                $"[MONEY_DESER_FAIL] NonNullableMoneyConverter could not deserialize " +
                $"adult_price/child_price JSON (length={json?.Length ?? 0}): {ex.GetType().Name} - {ex.Message}. " +
                $"Sanitized raw value: '{sanitized}'");
        }
        // Fallback - shouldn't happen for required columns. Renders as $0 USD
        // on the dashboard so the operator notices.
        return Money.Create(0, Currency.USD).Value;
    }

    private static string Sanitize(string? json)
    {
        if (string.IsNullOrEmpty(json)) return "<null-or-empty>";
        var sb = new System.Text.StringBuilder(json.Length);
        foreach (var c in json)
        {
            if (c < 0x20 || c == 0x7F)
            {
                sb.Append($"\\x{(int)c:X2}");
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }

    private class MoneyData
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
