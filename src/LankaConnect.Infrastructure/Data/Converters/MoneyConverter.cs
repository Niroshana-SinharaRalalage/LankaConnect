using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;
using System.Text.Json;

namespace LankaConnect.Infrastructure.Data.Converters;

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