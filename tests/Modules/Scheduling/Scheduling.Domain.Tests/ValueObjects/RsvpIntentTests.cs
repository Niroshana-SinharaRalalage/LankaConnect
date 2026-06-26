using LankaConnect.Modules.Scheduling.Domain.ValueObjects;

namespace LankaConnect.Modules.Scheduling.Domain.Tests.ValueObjects;

public class RsvpIntentTests
{
    [Fact]
    public void Create_WithUserId_Succeeds_AsPending()
    {
        var userId = Guid.NewGuid();
        var createdAt = DateTime.UtcNow;

        var result = RsvpIntent.Create(userId, createdAt);

        result.IsSuccess.Should().BeTrue();
        result.Value.UserId.Should().Be(userId);
        result.Value.Status.Should().Be(RsvpStatus.Pending);
        result.Value.CreatedAt.Should().Be(createdAt);
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyUserId_Fails()
    {
        var result = RsvpIntent.Create(Guid.Empty, DateTime.UtcNow);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Scheduling.Rsvp.MissingUserId");
    }

    [Fact]
    public void WithStatus_ReturnsNewInstance()
    {
        var original = RsvpIntent.Create(Guid.NewGuid(), DateTime.UtcNow).Value;
        var confirmed = original.WithStatus(RsvpStatus.Confirmed);

        confirmed.Status.Should().Be(RsvpStatus.Confirmed);
        original.Status.Should().Be(RsvpStatus.Pending); // unchanged
    }

    [Theory]
    [InlineData(RsvpStatus.Pending, true)]
    [InlineData(RsvpStatus.Confirmed, true)]
    [InlineData(RsvpStatus.Cancelled, false)]
    public void IsActive_OnlyForPendingOrConfirmed(RsvpStatus status, bool expected)
    {
        var intent = RsvpIntent.Create(Guid.NewGuid(), DateTime.UtcNow).Value.WithStatus(status);

        intent.IsActive.Should().Be(expected);
    }
}
