using LankaConnect.Domain.Common;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Business.Enums;

namespace LankaConnect.Domain.Business;

public class Review : BaseEntity
{
    public Rating Rating { get; private set; }
    public ReviewContent Content { get; private set; }
    public Guid BusinessId { get; private set; }
    public Guid ReviewerId { get; private set; }
    public ReviewStatus Status { get; private set; }
    public DateTime? ApprovedAt { get; private set; }
    public string? ModerationNotes { get; private set; }

    // Navigation properties
    public Business Business { get; private set; } = null!;

    private Review() // EF Core constructor
    {
        Rating = null!;
        Content = null!;
    }

    private Review(
        Rating rating,
        ReviewContent content,
        Guid businessId,
        Guid reviewerId) : base()
    {
        Rating = rating;
        Content = content;
        BusinessId = businessId;
        ReviewerId = reviewerId;
        Status = ReviewStatus.Pending;
        ApprovedAt = null;
        ModerationNotes = null;
    }

    public static Result<Review> Create(
        Rating rating,
        ReviewContent content,
        Guid businessId,
        Guid reviewerId)
    {
        if (rating == null)
            return Result<Review>.Failure("Rating is required");

        if (content == null)
            return Result<Review>.Failure("Review content is required");

        if (businessId == Guid.Empty)
            return Result<Review>.Failure("Business ID is required");

        if (reviewerId == Guid.Empty)
            return Result<Review>.Failure("Reviewer ID is required");

        var review = new Review(rating, content, businessId, reviewerId);

        return Result<Review>.Success(review);
    }

    public Result Update(Rating rating, ReviewContent content)
    {
        if (rating == null)
            return Result.Failure("Rating is required");

        if (content == null)
            return Result.Failure("Review content is required");

        if (Status == ReviewStatus.Approved)
            return Result.Failure("Cannot update approved review");

        Rating = rating;
        Content = content;
        Status = ReviewStatus.Pending; // Reset to pending after edit
        ApprovedAt = null;
        ModerationNotes = null;
        MarkAsUpdated();

        return Result.Success();
    }

    public Result Approve(string? moderationNotes = null)
    {
        if (Status == ReviewStatus.Approved)
            return Result.Failure("Review is already approved");

        Status = ReviewStatus.Approved;
        ApprovedAt = DateTime.UtcNow;
        ModerationNotes = moderationNotes?.Trim();
        MarkAsUpdated();

        return Result.Success();
    }

    public Result Reject(string moderationNotes)
    {
        if (string.IsNullOrWhiteSpace(moderationNotes))
            return Result.Failure("Moderation notes are required when rejecting a review");

        if (Status == ReviewStatus.Rejected)
            return Result.Failure("Review is already rejected");

        Status = ReviewStatus.Rejected;
        ApprovedAt = null;
        ModerationNotes = moderationNotes.Trim();
        MarkAsUpdated();

        return Result.Success();
    }

    public Result Restore()
    {
        if (Status != ReviewStatus.Rejected)
            return Result.Failure("Can only restore rejected reviews");

        Status = ReviewStatus.Pending;
        ApprovedAt = null;
        ModerationNotes = null;
        MarkAsUpdated();

        return Result.Success();
    }

    public bool IsVisible()
    {
        return Status == ReviewStatus.Approved;
    }
}