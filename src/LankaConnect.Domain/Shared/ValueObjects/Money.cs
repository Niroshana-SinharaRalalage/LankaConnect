using System.Globalization;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Domain.Shared.ValueObjects;

public class Money : ValueObject
{
    public decimal Amount { get; private set; }
    public Currency Currency { get; private set; }

    // EF Core parameterless constructor
    private Money()
    {
        // Required for EF Core JSON deserialization
    }

    public Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Result<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0)
            return Result<Money>.Failure("Amount cannot be negative");

        return Result<Money>.Success(new Money(amount, currency));
    }

    public Result<Money> Add(Money other)
    {
        if (Currency != other.Currency)
            return Result<Money>.Failure("Cannot add money with different currencies");

        return Create(Amount + other.Amount, Currency);
    }

    public Result<Money> Subtract(Money other)
    {
        if (Currency != other.Currency)
            return Result<Money>.Failure("Cannot subtract money with different currencies");

        if (Amount - other.Amount < 0)
            return Result<Money>.Failure("Subtraction would result in negative amount");

        return Create(Amount - other.Amount, Currency);
    }

    public Result<Money> Multiply(int multiplier)
    {
        if (multiplier < 0)
            return Result<Money>.Failure("Multiplier cannot be negative");

        return Create(Amount * multiplier, Currency);
    }

    public bool IsGreaterThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");

        return Amount > other.Amount;
    }

    public bool IsLessThan(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Cannot compare money with different currencies");

        return Amount < other.Amount;
    }

    public bool IsZero => Amount == 0;

    public override string ToString()
    {
        return Currency switch
        {
            Currency.USD => $"${Amount:F2}",
            Currency.LKR => $"Rs {Amount:N2}",
            Currency.GBP => $"£{Amount:F2}",
            Currency.EUR => $"€{Amount:F2}",
            Currency.CAD => $"C${Amount:F2}",
            Currency.AUD => $"A${Amount:F2}",
            _ => $"{Amount:F2} {Currency}"
        };
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public static Money Zero(Currency currency) => new Money(0, currency);
}