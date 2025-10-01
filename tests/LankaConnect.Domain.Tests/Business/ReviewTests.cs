using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;

namespace LankaConnect.Domain.Tests.Business;

public class ReviewTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var rating = Rating.Create(4).Value;
        var content = ReviewContent.Create("Great Service", "Great service, highly recommended!").Value;
        var businessId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var result = Review.Create(rating, content, businessId, reviewerId);

        Assert.True(result.IsSuccess);
        var review = result.Value;
        Assert.Equal(rating, review.Rating);
        Assert.Equal(content, review.Content);
        Assert.Equal(businessId, review.BusinessId);
        Assert.Equal(reviewerId, review.ReviewerId);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ApprovedAt);
        Assert.Null(review.ModerationNotes);
        Assert.NotEqual(Guid.Empty, review.Id);
    }

    [Fact]
    public void Create_WithNullRating_ShouldReturnFailure()
    {
        var content = ReviewContent.Create("Great Service", "Great service!").Value;
        var businessId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var result = Review.Create(null!, content, businessId, reviewerId);

        Assert.True(result.IsFailure);
        Assert.Contains("Rating is required", result.Errors);
    }

    [Fact]
    public void Create_WithNullContent_ShouldReturnFailure()
    {
        var rating = Rating.Create(4).Value;
        var businessId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var result = Review.Create(rating, null!, businessId, reviewerId);

        Assert.True(result.IsFailure);
        Assert.Contains("Review content is required", result.Errors);
    }

    [Fact]
    public void Create_WithEmptyBusinessId_ShouldReturnFailure()
    {
        var rating = Rating.Create(4).Value;
        var content = ReviewContent.Create("Great Service", "Great service!").Value;
        var reviewerId = Guid.NewGuid();

        var result = Review.Create(rating, content, Guid.Empty, reviewerId);

        Assert.True(result.IsFailure);
        Assert.Contains("Business ID is required", result.Errors);
    }

    [Fact]
    public void Create_WithEmptyReviewerId_ShouldReturnFailure()
    {
        var rating = Rating.Create(4).Value;
        var content = ReviewContent.Create("Great Service", "Great service!").Value;
        var businessId = Guid.NewGuid();

        var result = Review.Create(rating, content, businessId, Guid.Empty);

        Assert.True(result.IsFailure);
        Assert.Contains("Reviewer ID is required", result.Errors);
    }

    [Fact]
    public void Update_WithValidData_ShouldUpdateSuccessfully()
    {
        var review = CreateValidReview();
        var newRating = Rating.Create(5).Value;
        var newContent = ReviewContent.Create("Updated Review", "Updated review: Excellent service!").Value;

        var result = review.Update(newRating, newContent);

        Assert.True(result.IsSuccess);
        Assert.Equal(newRating, review.Rating);
        Assert.Equal(newContent, review.Content);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ApprovedAt);
        Assert.Null(review.ModerationNotes);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void Update_WithNullRating_ShouldReturnFailure()
    {
        var review = CreateValidReview();
        var newContent = ReviewContent.Create("Updated Content", "Updated content").Value;

        var result = review.Update(null!, newContent);

        Assert.True(result.IsFailure);
        Assert.Contains("Rating is required", result.Errors);
    }

    [Fact]
    public void Update_WithNullContent_ShouldReturnFailure()
    {
        var review = CreateValidReview();
        var newRating = Rating.Create(5).Value;

        var result = review.Update(newRating, null!);

        Assert.True(result.IsFailure);
        Assert.Contains("Review content is required", result.Errors);
    }

    [Fact]
    public void Update_WhenReviewIsApproved_ShouldReturnFailure()
    {
        var review = CreateValidReview();
        review.Approve();
        var newRating = Rating.Create(5).Value;
        var newContent = ReviewContent.Create("Updated Content", "Updated content").Value;

        var result = review.Update(newRating, newContent);

        Assert.True(result.IsFailure);
        Assert.Contains("Cannot update approved review", result.Errors);
    }

    [Fact]
    public void Update_ShouldResetStatusToPending()
    {
        var review = CreateValidReview();
        review.Reject("Test rejection");
        var newRating = Rating.Create(5).Value;
        var newContent = ReviewContent.Create("Updated Content", "Updated content").Value;

        var result = review.Update(newRating, newContent);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ApprovedAt);
        Assert.Null(review.ModerationNotes);
    }

    [Fact]
    public void Approve_WhenPending_ShouldApproveSuccessfully()
    {
        var review = CreateValidReview();
        var moderationNotes = "Looks good, approved";

        var result = review.Approve(moderationNotes);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.NotNull(review.ApprovedAt);
        Assert.Equal(moderationNotes, review.ModerationNotes);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void Approve_WithoutModerationNotes_ShouldApproveSuccessfully()
    {
        var review = CreateValidReview();

        var result = review.Approve();

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Approved, review.Status);
        Assert.NotNull(review.ApprovedAt);
        Assert.Null(review.ModerationNotes);
    }

    [Fact]
    public void Approve_WithWhitespaceModerationNotes_ShouldTrimNotes()
    {
        var review = CreateValidReview();
        var moderationNotes = "  Approved after review  ";

        var result = review.Approve(moderationNotes);

        Assert.True(result.IsSuccess);
        Assert.Equal("Approved after review", review.ModerationNotes);
    }

    [Fact]
    public void Approve_WhenAlreadyApproved_ShouldReturnFailure()
    {
        var review = CreateValidReview();
        review.Approve();

        var result = review.Approve();

        Assert.True(result.IsFailure);
        Assert.Contains("Review is already approved", result.Errors);
    }

    [Fact]
    public void Reject_WithValidNotes_ShouldRejectSuccessfully()
    {
        var review = CreateValidReview();
        var moderationNotes = "Contains inappropriate content";

        var result = review.Reject(moderationNotes);

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Rejected, review.Status);
        Assert.Null(review.ApprovedAt);
        Assert.Equal(moderationNotes, review.ModerationNotes);
        Assert.NotNull(review.UpdatedAt);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Reject_WithInvalidModerationNotes_ShouldReturnFailure(string moderationNotes)
    {
        var review = CreateValidReview();

        var result = review.Reject(moderationNotes);

        Assert.True(result.IsFailure);
        Assert.Contains("Moderation notes are required when rejecting a review", result.Errors);
    }

    [Fact]
    public void Reject_WithWhitespaceModerationNotes_ShouldTrimNotes()
    {
        var review = CreateValidReview();
        var moderationNotes = "  Rejected due to policy violation  ";

        var result = review.Reject(moderationNotes);

        Assert.True(result.IsSuccess);
        Assert.Equal("Rejected due to policy violation", review.ModerationNotes);
    }

    [Fact]
    public void Reject_WhenAlreadyRejected_ShouldReturnFailure()
    {
        var review = CreateValidReview();
        review.Reject("Initial rejection");

        var result = review.Reject("Second rejection");

        Assert.True(result.IsFailure);
        Assert.Contains("Review is already rejected", result.Errors);
    }

    [Fact]
    public void Restore_WhenRejected_ShouldRestoreSuccessfully()
    {
        var review = CreateValidReview();
        review.Reject("Initial rejection");

        var result = review.Restore();

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ApprovedAt);
        Assert.Null(review.ModerationNotes);
        Assert.NotNull(review.UpdatedAt);
    }

    [Fact]
    public void Restore_WhenNotRejected_ShouldReturnFailure()
    {
        var review = CreateValidReview(); // Pending status

        var result = review.Restore();

        Assert.True(result.IsFailure);
        Assert.Contains("Can only restore rejected reviews", result.Errors);
    }

    [Fact]
    public void Restore_WhenApproved_ShouldReturnFailure()
    {
        var review = CreateValidReview();
        review.Approve();

        var result = review.Restore();

        Assert.True(result.IsFailure);
        Assert.Contains("Can only restore rejected reviews", result.Errors);
    }

    [Fact]
    public void IsVisible_WhenApproved_ShouldReturnTrue()
    {
        var review = CreateValidReview();
        review.Approve();

        var isVisible = review.IsVisible();

        Assert.True(isVisible);
    }

    [Fact]
    public void IsVisible_WhenPending_ShouldReturnFalse()
    {
        var review = CreateValidReview();

        var isVisible = review.IsVisible();

        Assert.False(isVisible);
    }

    [Fact]
    public void IsVisible_WhenRejected_ShouldReturnFalse()
    {
        var review = CreateValidReview();
        review.Reject("Violation of terms");

        var isVisible = review.IsVisible();

        Assert.False(isVisible);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues()
    {
        var review = CreateValidReview();

        Assert.Equal(ReviewStatus.Pending, review.Status);
        Assert.Null(review.ApprovedAt);
        Assert.Null(review.ModerationNotes);
        Assert.True(review.CreatedAt > DateTime.MinValue);
        Assert.Null(review.UpdatedAt);
        Assert.NotEqual(Guid.Empty, review.Id);
    }

    [Fact]
    public void StatusTransition_PendingToApproved_ShouldWork()
    {
        var review = CreateValidReview();
        Assert.Equal(ReviewStatus.Pending, review.Status);

        review.Approve();
        Assert.Equal(ReviewStatus.Approved, review.Status);
    }

    [Fact]
    public void StatusTransition_PendingToRejected_ShouldWork()
    {
        var review = CreateValidReview();
        Assert.Equal(ReviewStatus.Pending, review.Status);

        review.Reject("Policy violation");
        Assert.Equal(ReviewStatus.Rejected, review.Status);
    }

    [Fact]
    public void StatusTransition_RejectedToPending_ShouldWork()
    {
        var review = CreateValidReview();
        review.Reject("Initial rejection");
        Assert.Equal(ReviewStatus.Rejected, review.Status);

        review.Restore();
        Assert.Equal(ReviewStatus.Pending, review.Status);
    }

    [Fact]
    public void StatusTransition_RejectedToApproved_ShouldWorkAfterRestore()
    {
        var review = CreateValidReview();
        review.Reject("Initial rejection");
        review.Restore();

        var result = review.Approve();

        Assert.True(result.IsSuccess);
        Assert.Equal(ReviewStatus.Approved, review.Status);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create_WithAllValidRatings_ShouldReturnSuccess(int ratingValue)
    {
        var rating = Rating.Create(ratingValue).Value;
        var content = ReviewContent.Create("Test Review", "Test review content").Value;
        var businessId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        var result = Review.Create(rating, content, businessId, reviewerId);

        Assert.True(result.IsSuccess);
        Assert.Equal(ratingValue, result.Value.Rating.Value);
    }

    [Fact]
    public void Approve_ShouldSetCorrectApprovalTimestamp()
    {
        var review = CreateValidReview();
        var beforeApproval = DateTime.UtcNow;

        review.Approve();

        var afterApproval = DateTime.UtcNow;
        Assert.NotNull(review.ApprovedAt);
        Assert.True(review.ApprovedAt >= beforeApproval);
        Assert.True(review.ApprovedAt <= afterApproval);
    }

    [Fact]
    public void Reject_ShouldClearApprovalTimestamp()
    {
        var review = CreateValidReview();
        review.Approve(); // First approve
        Assert.NotNull(review.ApprovedAt);

        review.Restore(); // Restore to pending
        review.Reject("Rejected after approval"); // Then reject

        Assert.Null(review.ApprovedAt);
    }

    [Fact]
    public void BusinessId_ShouldBeSetCorrectly()
    {
        var businessId = Guid.NewGuid();
        var rating = Rating.Create(4).Value;
        var content = ReviewContent.Create("Test Content", "Test content").Value;
        var reviewerId = Guid.NewGuid();

        var review = Review.Create(rating, content, businessId, reviewerId).Value;

        Assert.Equal(businessId, review.BusinessId);
    }

    [Fact]
    public void ReviewerId_ShouldBeSetCorrectly()
    {
        var reviewerId = Guid.NewGuid();
        var rating = Rating.Create(4).Value;
        var content = ReviewContent.Create("Test Content", "Test content").Value;
        var businessId = Guid.NewGuid();

        var review = Review.Create(rating, content, businessId, reviewerId).Value;

        Assert.Equal(reviewerId, review.ReviewerId);
    }

    private static Review CreateValidReview()
    {
        var rating = Rating.Create(4).Value;
        var content = ReviewContent.Create("Great Service", "Great service, highly recommended!").Value;
        var businessId = Guid.NewGuid();
        var reviewerId = Guid.NewGuid();

        return Review.Create(rating, content, businessId, reviewerId).Value;
    }
}