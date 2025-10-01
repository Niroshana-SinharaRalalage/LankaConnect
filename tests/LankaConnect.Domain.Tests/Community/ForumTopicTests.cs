using LankaConnect.Domain.Community;
using LankaConnect.Domain.Community.ValueObjects;
using LankaConnect.Domain.Community.Enums;

namespace LankaConnect.Domain.Tests.Community;

public class ForumTopicTests
{
    [Fact]
    public void Create_WithValidData_ShouldReturnSuccess()
    {
        var title = ForumTitle.Create("Need help with visa application").Value;
        var content = PostContent.Create("I'm applying for a work visa and need some guidance. Has anyone been through this process recently?").Value;
        var authorId = Guid.NewGuid();
        var forumId = Guid.NewGuid();
        
        var result = ForumTopic.Create(title, content, authorId, forumId, ForumCategory.Immigration);
        
        Assert.True(result.IsSuccess);
        var topic = result.Value;
        Assert.Equal(title, topic.Title);
        Assert.Equal(content, topic.Content);
        Assert.Equal(authorId, topic.AuthorId);
        Assert.Equal(forumId, topic.ForumId);
        Assert.Equal(ForumCategory.Immigration, topic.Category);
        Assert.Equal(TopicStatus.Active, topic.Status);
        Assert.Equal(0, topic.ViewCount);
        Assert.False(topic.IsPinned);
        Assert.NotEqual(Guid.Empty, topic.Id);
    }

    [Fact]
    public void Create_WithEmptyAuthorId_ShouldReturnFailure()
    {
        var title = ForumTitle.Create("Test Topic").Value;
        var content = PostContent.Create("Test content").Value;
        
        var result = ForumTopic.Create(title, content, Guid.Empty, Guid.NewGuid(), ForumCategory.General);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Author ID is required", result.Errors);
    }

    [Fact]
    public void Create_WithEmptyForumId_ShouldReturnFailure()
    {
        var title = ForumTitle.Create("Test Topic").Value;
        var content = PostContent.Create("Test content").Value;
        
        var result = ForumTopic.Create(title, content, Guid.NewGuid(), Guid.Empty, ForumCategory.General);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Forum ID is required", result.Errors);
    }

    [Fact]
    public void AddReply_WithValidData_ShouldAddReply()
    {
        var topic = CreateValidTopic();
        var replyContent = PostContent.Create("This is a helpful reply to the original post.").Value;
        var replyAuthorId = Guid.NewGuid();
        
        var result = topic.AddReply(replyContent, replyAuthorId);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(1, topic.ReplyCount);
        Assert.Single(topic.Replies);
        Assert.Equal(replyContent, topic.Replies.First().Content);
        Assert.Equal(replyAuthorId, topic.Replies.First().AuthorId);
        Assert.NotNull(topic.UpdatedAt);
    }

    [Fact]
    public void AddReply_WithEmptyAuthorId_ShouldReturnFailure()
    {
        var topic = CreateValidTopic();
        var replyContent = PostContent.Create("Reply content").Value;
        
        var result = topic.AddReply(replyContent, Guid.Empty);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Author ID is required", result.Errors);
    }

    [Fact]
    public void AddReply_WhenTopicIsLocked_ShouldReturnFailure()
    {
        var topic = CreateValidTopic();
        topic.Lock(Guid.NewGuid(), "Inappropriate content");
        
        var result = topic.AddReply(PostContent.Create("Reply").Value, Guid.NewGuid());
        
        Assert.True(result.IsFailure);
        Assert.Contains("Cannot reply to locked topic", result.Errors);
    }

    [Fact]
    public void Pin_WhenUnpinned_ShouldPinTopic()
    {
        var topic = CreateValidTopic();
        var moderatorId = Guid.NewGuid();
        
        topic.Pin(moderatorId);
        
        Assert.True(topic.IsPinned);
        Assert.NotNull(topic.UpdatedAt);
    }

    [Fact]
    public void Unpin_WhenPinned_ShouldUnpinTopic()
    {
        var topic = CreateValidTopic();
        var moderatorId = Guid.NewGuid();
        topic.Pin(moderatorId);
        
        topic.Unpin(moderatorId);
        
        Assert.False(topic.IsPinned);
        Assert.NotNull(topic.UpdatedAt);
    }

    [Fact]
    public void Lock_WithValidReason_ShouldLockTopic()
    {
        var topic = CreateValidTopic();
        var moderatorId = Guid.NewGuid();
        var reason = "Off-topic discussion";
        
        var result = topic.Lock(moderatorId, reason);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(TopicStatus.Locked, topic.Status);
        Assert.Equal(reason, topic.LockReason);
        Assert.NotNull(topic.UpdatedAt);
    }

    [Fact]
    public void Lock_WithEmptyReason_ShouldReturnFailure()
    {
        var topic = CreateValidTopic();
        
        var result = topic.Lock(Guid.NewGuid(), "");
        
        Assert.True(result.IsFailure);
        Assert.Contains("Lock reason is required", result.Errors);
    }

    [Fact]
    public void Unlock_WhenLocked_ShouldUnlockTopic()
    {
        var topic = CreateValidTopic();
        topic.Lock(Guid.NewGuid(), "Test reason");
        
        topic.Unlock(Guid.NewGuid());
        
        Assert.Equal(TopicStatus.Active, topic.Status);
        Assert.Null(topic.LockReason);
        Assert.NotNull(topic.UpdatedAt);
    }

    [Fact]
    public void IncrementViewCount_ShouldIncreaseViewCount()
    {
        var topic = CreateValidTopic();
        var initialCount = topic.ViewCount;
        
        topic.IncrementViewCount();
        
        Assert.Equal(initialCount + 1, topic.ViewCount);
        Assert.NotNull(topic.UpdatedAt);
    }

    [Fact]
    public void UpdateContent_WithValidContent_ShouldUpdateContent()
    {
        var topic = CreateValidTopic();
        var newContent = PostContent.Create("Updated content with more details.").Value;
        
        var result = topic.UpdateContent(newContent, topic.AuthorId);
        
        Assert.True(result.IsSuccess);
        Assert.Equal(newContent, topic.Content);
        Assert.NotNull(topic.UpdatedAt);
    }

    [Fact]
    public void UpdateContent_WhenNotAuthor_ShouldReturnFailure()
    {
        var topic = CreateValidTopic();
        var newContent = PostContent.Create("Updated content").Value;
        var differentUserId = Guid.NewGuid();
        
        var result = topic.UpdateContent(newContent, differentUserId);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Only the author can update the topic content", result.Errors);
    }

    [Fact]
    public void UpdateContent_WhenTopicIsLocked_ShouldReturnFailure()
    {
        var topic = CreateValidTopic();
        topic.Lock(Guid.NewGuid(), "Locked for review");
        
        var result = topic.UpdateContent(PostContent.Create("New content").Value, topic.AuthorId);
        
        Assert.True(result.IsFailure);
        Assert.Contains("Cannot update locked topic", result.Errors);
    }

    private static ForumTopic CreateValidTopic()
    {
        var title = ForumTitle.Create("Test Topic").Value;
        var content = PostContent.Create("Test content for the topic").Value;
        return ForumTopic.Create(title, content, Guid.NewGuid(), Guid.NewGuid(), ForumCategory.General).Value;
    }
}