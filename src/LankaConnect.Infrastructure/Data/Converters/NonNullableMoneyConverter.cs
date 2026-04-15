using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Data.Converters;

/// <summary>
/// Value converter for non-nullable Money property to JSON string.
/// Use this for required Money columns (e.g., TicketTier.AdultPrice).
/// For nullable Money? columns, use MoneyConverter instead.
/// </summary>
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
        var data = JsonSerializer.Deserialize<MoneyData>(json);
        if (data != null && Enum.TryParse<Currency>(data.Currency, out var currency))
        {
            return Money.Create(data.Amount, currency).Value;
        }
        // Fallback — shouldn't happen for required columns
        return Money.Create(0, Currency.USD).Value;
    }

    private class MoneyData
    {
        public decimal Amount { get; set; }
        public string Currency { get; set; } = string.Empty;
    }
}
