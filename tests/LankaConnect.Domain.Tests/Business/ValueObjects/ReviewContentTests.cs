using LankaConnect.Domain.Business.ValueObjects;

namespace LankaConnect.Domain.Tests.Business.ValueObjects;

public class ReviewContentTests
{
    [Fact]
    public void Create_WithValidTitleAndContent_ShouldReturnSuccess()
    {
        var title = "Excellent Service";
        var content = "I had a wonderful experience with this business. The staff was professional and the service was outstanding.";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        var reviewContent = result.Value;
        Assert.Equal(title, reviewContent.Title);
        Assert.Equal(content, reviewContent.Content);
        Assert.Null(reviewContent.Pros);
        Assert.Null(reviewContent.Cons);
    }

    [Fact]
    public void Create_WithTitleContentAndProsAndCons_ShouldReturnSuccess()
    {
        var title = "Mixed Experience";
        var content = "Overall decent experience with some positive and negative aspects.";
        var pros = new List<string> { "Great food", "Friendly staff", "Good location" };
        var cons = new List<string> { "Long wait time", "Expensive", "Limited parking" };

        var result = ReviewContent.Create(title, content, pros, cons);

        Assert.True(result.IsSuccess);
        var reviewContent = result.Value;
        Assert.Equal(title, reviewContent.Title);
        Assert.Equal(content, reviewContent.Content);
        Assert.Equal(pros, reviewContent.Pros);
        Assert.Equal(cons, reviewContent.Cons);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidTitle_ShouldReturnFailure(string title)
    {
        var content = "Valid content for the review";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsFailure);
        Assert.Contains("Review title is required", result.Errors);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithInvalidContent_ShouldReturnFailure(string content)
    {
        var title = "Valid Title";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsFailure);
        Assert.Contains("Review content is required", result.Errors);
    }

    [Fact]
    public void Create_WithTooLongContent_ShouldReturnFailure()
    {
        var title = "Valid Title";
        var content = new string('a', 2001);

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsFailure);
        Assert.Contains("Review content cannot exceed 2000 characters", result.Errors);
    }

    [Fact]
    public void Create_WithMaxLengthContent_ShouldReturnSuccess()
    {
        var title = "Valid Title";
        var content = new string('a', 2000);

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Equal(content, result.Value.Content);
    }

    [Fact]
    public void Create_ShouldTrimWhitespaceFromTitleAndContent()
    {
        var title = "  Excellent Service  ";
        var content = "  Great experience with professional staff.  ";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        var reviewContent = result.Value;
        Assert.Equal("Excellent Service", reviewContent.Title);
        Assert.Equal("Great experience with professional staff.", reviewContent.Content);
    }

    [Fact]
    public void Create_WithEmptyProsAndCons_ShouldReturnSuccess()
    {
        var title = "Review Title";
        var content = "Review content";
        var pros = new List<string>();
        var cons = new List<string>();

        var result = ReviewContent.Create(title, content, pros, cons);

        Assert.True(result.IsSuccess);
        var reviewContent = result.Value;
        Assert.Equal(pros, reviewContent.Pros);
        Assert.Equal(cons, reviewContent.Cons);
    }

    [Fact]
    public void Create_WithOnlyPros_ShouldReturnSuccess()
    {
        var title = "Positive Review";
        var content = "Everything was excellent!";
        var pros = new List<string> { "Great food", "Excellent service", "Nice atmosphere" };

        var result = ReviewContent.Create(title, content, pros);

        Assert.True(result.IsSuccess);
        var reviewContent = result.Value;
        Assert.Equal(pros, reviewContent.Pros);
        Assert.Null(reviewContent.Cons);
    }

    [Fact]
    public void Create_WithOnlyCons_ShouldReturnSuccess()
    {
        var title = "Disappointing Experience";
        var content = "Several issues encountered during the visit.";
        var cons = new List<string> { "Poor service", "Cold food", "Unclean facilities" };

        var result = ReviewContent.Create(title, content, cons: cons);

        Assert.True(result.IsSuccess);
        var reviewContent = result.Value;
        Assert.Null(reviewContent.Pros);
        Assert.Equal(cons, reviewContent.Cons);
    }

    [Fact]
    public void Equality_WithSameTitleAndContent_ShouldBeEqual()
    {
        var title = "Same Title";
        var content = "Same content text";

        var reviewContent1 = ReviewContent.Create(title, content).Value;
        var reviewContent2 = ReviewContent.Create(title, content).Value;

        Assert.Equal(reviewContent1, reviewContent2);
    }

    [Fact]
    public void Equality_WithDifferentTitles_ShouldNotBeEqual()
    {
        var content = "Same content text";

        var reviewContent1 = ReviewContent.Create("Title 1", content).Value;
        var reviewContent2 = ReviewContent.Create("Title 2", content).Value;

        Assert.NotEqual(reviewContent1, reviewContent2);
    }

    [Fact]
    public void Equality_WithDifferentContent_ShouldNotBeEqual()
    {
        var title = "Same Title";

        var reviewContent1 = ReviewContent.Create(title, "Content 1").Value;
        var reviewContent2 = ReviewContent.Create(title, "Content 2").Value;

        Assert.NotEqual(reviewContent1, reviewContent2);
    }

    [Fact]
    public void Equality_WithSameProsAndCons_ShouldBeEqual()
    {
        var title = "Title";
        var content = "Content";
        var pros = new List<string> { "Pro 1", "Pro 2" };
        var cons = new List<string> { "Con 1", "Con 2" };

        var reviewContent1 = ReviewContent.Create(title, content, pros, cons).Value;
        var reviewContent2 = ReviewContent.Create(title, content, pros, cons).Value;

        Assert.Equal(reviewContent1, reviewContent2);
    }

    [Fact]
    public void Equality_WithDifferentPros_ShouldNotBeEqual()
    {
        var title = "Title";
        var content = "Content";

        var reviewContent1 = ReviewContent.Create(title, content, new List<string> { "Pro 1" }).Value;
        var reviewContent2 = ReviewContent.Create(title, content, new List<string> { "Pro 2" }).Value;

        Assert.NotEqual(reviewContent1, reviewContent2);
    }

    [Fact]
    public void Equality_WithDifferentCons_ShouldNotBeEqual()
    {
        var title = "Title";
        var content = "Content";

        var reviewContent1 = ReviewContent.Create(title, content, cons: new List<string> { "Con 1" }).Value;
        var reviewContent2 = ReviewContent.Create(title, content, cons: new List<string> { "Con 2" }).Value;

        Assert.NotEqual(reviewContent1, reviewContent2);
    }

    [Fact]
    public void Equality_WithOneHavingProsOtherNot_ShouldNotBeEqual()
    {
        var title = "Title";
        var content = "Content";

        var reviewContent1 = ReviewContent.Create(title, content, new List<string> { "Pro 1" }).Value;
        var reviewContent2 = ReviewContent.Create(title, content).Value;

        Assert.NotEqual(reviewContent1, reviewContent2);
    }

    [Fact]
    public void GetHashCode_WithSameValues_ShouldBeEqual()
    {
        var title = "Title";
        var content = "Content";
        var pros = new List<string> { "Pro 1" };

        var reviewContent1 = ReviewContent.Create(title, content, pros).Value;
        var reviewContent2 = ReviewContent.Create(title, content, pros).Value;

        Assert.Equal(reviewContent1.GetHashCode(), reviewContent2.GetHashCode());
    }

    [Fact]
    public void Create_WithComplexContent_ShouldReturnSuccess()
    {
        var title = "Comprehensive Review";
        var content = @"I visited this restaurant last weekend and had a mixed experience. 
                        The ambiance was lovely and the staff was welcoming. However, 
                        there were some issues with the service timing that I'd like to mention.";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Contains("restaurant", result.Value.Content);
        Assert.Contains("mixed experience", result.Value.Content);
    }

    [Fact]
    public void Create_WithUnicodeCharacters_ShouldReturnSuccess()
    {
        var title = "විශිෂ්ට සේවාව"; // Sinhala characters
        var content = "මෙම ව්‍යාපාරය ඉතා හොඳ සේවාවක් සපයයි। 🌟 Excellent service with emoji support!";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Equal(title, result.Value.Title);
        Assert.Equal(content, result.Value.Content);
    }

    [Fact]
    public void Create_WithSpecialCharacters_ShouldReturnSuccess()
    {
        var title = "Review with Special Chars: @#$%^&*()";
        var content = "Content with quotes: \"Great!\" and apostrophe's and <tags>";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Equal(title, result.Value.Title);
        Assert.Equal(content, result.Value.Content);
    }

    [Fact]
    public void Create_WithNumericContent_ShouldReturnSuccess()
    {
        var title = "Rating: 5/5 stars";
        var content = "Price: $25.99, Wait time: 15 minutes, Rating: 4.8/5.0";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Contains("$25.99", result.Value.Content);
        Assert.Contains("4.8/5.0", result.Value.Content);
    }

    [Fact]
    public void Create_WithLongProsList_ShouldReturnSuccess()
    {
        var title = "Detailed Review";
        var content = "Comprehensive review with many pros.";
        var pros = Enumerable.Range(1, 20).Select(i => $"Pro number {i}").ToList();

        var result = ReviewContent.Create(title, content, pros);

        Assert.True(result.IsSuccess);
        Assert.Equal(20, result.Value.Pros!.Count);
        Assert.Equal("Pro number 1", result.Value.Pros.First());
        Assert.Equal("Pro number 20", result.Value.Pros.Last());
    }

    [Fact]
    public void Create_WithLongConsList_ShouldReturnSuccess()
    {
        var title = "Critical Review";
        var content = "Review with many concerns.";
        var cons = Enumerable.Range(1, 15).Select(i => $"Issue number {i}").ToList();

        var result = ReviewContent.Create(title, content, cons: cons);

        Assert.True(result.IsSuccess);
        Assert.Equal(15, result.Value.Cons!.Count);
        Assert.Equal("Issue number 1", result.Value.Cons.First());
        Assert.Equal("Issue number 15", result.Value.Cons.Last());
    }

    [Theory]
    [InlineData("Short")]
    [InlineData("A")]
    [InlineData("Very Long Title That Goes On And On But Still Within Reasonable Limits For A Review Title")]
    public void Create_WithVariousTitleLengths_ShouldReturnSuccess(string title)
    {
        var content = "Standard review content that provides adequate information.";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Equal(title, result.Value.Title);
    }

    [Fact]
    public void Create_WithBoundaryContentLength_ShouldReturnSuccess()
    {
        var title = "Boundary Test";
        var content = new string('x', 1999); // One character less than limit

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Equal(1999, result.Value.Content.Length);
    }

    [Fact]
    public void Create_WithMultilineContent_ShouldReturnSuccess()
    {
        var title = "Multiline Review";
        var content = @"First line of review.
                        Second line with more details.
                        Third line with conclusion.";

        var result = ReviewContent.Create(title, content);

        Assert.True(result.IsSuccess);
        Assert.Contains("First line", result.Value.Content);
        Assert.Contains("Second line", result.Value.Content);
        Assert.Contains("Third line", result.Value.Content);
    }
}