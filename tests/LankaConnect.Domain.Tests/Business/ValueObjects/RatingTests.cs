using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class RatingTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_WithValidRating_ShouldReturnSuccess(int value)
    {
        var result = Rating.Create(value);

        Assert.True(result.IsSuccess);
        Assert.Equal(value, result.Value.Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithBelowMinimumRating_ShouldReturnFailure(int value)
    {
        var result = Rating.Create(value);

        Assert.True(result.IsFailure);
        Assert.Contains("Rating must be between 1 and 5", result.Errors);
    }

    [Theory]
    [InlineData(6)]
    [InlineData(10)]
    [InlineData(100)]
    public void Create_WithAboveMaximumRating_ShouldReturnFailure(int value)
    {
        var result = Rating.Create(value);

        Assert.True(result.IsFailure);
        Assert.Contains("Rating must be between 1 and 5", result.Errors);
    }

    [Fact]
    public void Create_WithMinimumValidRating_ShouldReturnSuccess()
    {
        var result = Rating.Create(1);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.Value);
    }

    [Fact]
    public void Create_WithMaximumValidRating_ShouldReturnSuccess()
    {
        var result = Rating.Create(5);

        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Value);
    }

    [Fact]
    public void Equality_WithSameValue_ShouldBeEqual()
    {
        var rating1 = Rating.Create(4).Value;
        var rating2 = Rating.Create(4).Value;

        Assert.Equal(rating1, rating2);
    }

    [Fact]
    public void Equality_WithDifferentValues_ShouldNotBeEqual()
    {
        var rating1 = Rating.Create(3).Value;
        var rating2 = Rating.Create(4).Value;

        Assert.NotEqual(rating1, rating2);
    }

    [Fact]
    public void GetHashCode_WithSameValue_ShouldBeEqual()
    {
        var rating1 = Rating.Create(4).Value;
        var rating2 = Rating.Create(4).Value;

        Assert.Equal(rating1.GetHashCode(), rating2.GetHashCode());
    }

    [Fact]
    public void GetHashCode_WithDifferentValues_ShouldNotBeEqual()
    {
        var rating1 = Rating.Create(3).Value;
        var rating2 = Rating.Create(4).Value;

        Assert.NotEqual(rating1.GetHashCode(), rating2.GetHashCode());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void ToString_ShouldReturnRatingDescription(int value)
    {
        var rating = Rating.Create(value).Value;
        
        // Since Rating doesn't override ToString, it will use the default value object behavior
        Assert.Contains(value.ToString(), rating.ToString());
    }

    [Fact]
    public void Value_ShouldReturnCorrectValue()
    {
        var expectedValue = 4;
        var rating = Rating.Create(expectedValue).Value;

        Assert.Equal(expectedValue, rating.Value);
    }

    [Fact]
    public void Create_MultipleInstancesWithSameValue_ShouldAllBeEqual()
    {
        var ratings = new List<Rating>();
        for (int i = 0; i < 10; i++)
        {
            ratings.Add(Rating.Create(3).Value);
        }

        for (int i = 1; i < ratings.Count; i++)
        {
            Assert.Equal(ratings[0], ratings[i]);
        }
    }

    [Fact]
    public void Create_AllValidRatings_ShouldBeInAscendingOrder()
    {
        var ratings = new List<Rating>
        {
            Rating.Create(1).Value,
            Rating.Create(2).Value,
            Rating.Create(3).Value,
            Rating.Create(4).Value,
            Rating.Create(5).Value
        };

        for (int i = 1; i < ratings.Count; i++)
        {
            Assert.True(ratings[i].Value > ratings[i - 1].Value);
        }
    }
}