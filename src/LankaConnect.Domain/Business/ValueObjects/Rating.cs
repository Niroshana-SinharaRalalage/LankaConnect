using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Business.ValueObjects;

public class Rating : ValueObject
{
    public int Value { get; }
    
    private Rating(int value)
    {
        Value = value;
    }
    
    public static Result<Rating> Create(int value)
    {
        if (value < 1 || value > 5)
            return Result<Rating>.Failure("Rating must be between 1 and 5");
            
        return Result<Rating>.Success(new Rating(value));
    }
    
    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString()
    {
        return Value switch
        {
            1 => "1 star - Poor",
            2 => "2 stars - Fair", 
            3 => "3 stars - Good",
            4 => "4 stars - Very Good",
            5 => "5 stars - Excellent",
            _ => $"{Value} stars"
        };
    }
}