using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using LankaConnect.SharedKernel.Money;
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

            var currencyMaybe = Currency.TryFromCode(data.Currency);
            if (currencyMaybe.HasValue)
            {
                return new Money(data.Amount, currencyMaybe.Value);
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