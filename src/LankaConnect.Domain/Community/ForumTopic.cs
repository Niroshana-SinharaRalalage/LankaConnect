using LankaConnect.Domain.Common;
using LankaConnect.Domain.Community.ValueObjects;
using LankaConnect.Domain.Community.Enums;

namespace LankaConnect.Domain.Community;

public class ForumTopic : BaseEntity
{
    private readonly List<Reply> _replies = new();

    public ForumTitle Title { get; private set; }
    public PostContent Content { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid ForumId { get; private set; }
    public ForumCategory Category { get; private set; }
    public TopicStatus Status { get; private set; }
    public bool IsPinned { get; private set; }
    public int ViewCount { get; private set; }
    public string? LockReason { get; private set; }

    public IReadOnlyList<Reply> Replies => _replies.AsReadOnly();
    public int ReplyCount => _replies.Count;

    // EF Core constructor
    private ForumTopic() 
    {
        Title = null!;
        Content = null!;
    }

    private ForumTopic(ForumTitle title, PostContent content, Guid authorId, Guid forumId, ForumCategory category)
    {
        Title = title;
        Content = content;
        AuthorId = authorId;
        ForumId = forumId;
        Category = category;
        Status = TopicStatus.Active;
        IsPinned = false;
        ViewCount = 0;
    }

    public static Result<ForumTopic> Create(ForumTitle title, PostContent content, Guid authorId, Guid forumId, ForumCategory category)
    {
        if (title == null)
            return Result<ForumTopic>.Failure("Title is required");

        if (content == null)
            return Result<ForumTopic>.Failure("Content is required");

        if (authorId == Guid.Empty)
            return Result<ForumTopic>.Failure("Author ID is required");

        if (forumId == Guid.Empty)
            return Result<ForumTopic>.Failure("Forum ID is required");

        var topic = new ForumTopic(title, content, authorId, forumId, category);
        return Result<ForumTopic>.Success(topic);
    }

    public Result AddReply(PostContent content, Guid authorId, Guid? parentReplyId = null)
    {
        if (Status == TopicStatus.Locked)
            return Result.Failure("Cannot reply to locked topic");

        if (Status == TopicStatus.Deleted)
            return Result.Failure("Cannot reply to deleted topic");

        if (authorId == Guid.Empty)
            return Result.Failure("Author ID is required");

        var replyResult = Reply.Create(Id, content, authorId, parentReplyId);
        if (replyResult.IsFailure)
            return Result.Failure(replyResult.Errors);

        _replies.Add(replyResult.Value);
        MarkAsUpdated();
        return Result.Success();
    }

    public Result UpdateContent(PostContent newContent, Guid editorId)
    {
        if (newContent == null)
            return Result.Failure("Content is required");

        if (editorId != AuthorId)
            return Result.Failure("Only the author can update the topic content");

        if (Status == TopicStatus.Locked)
            return Result.Failure("Cannot update locked topic");

        if (Status == TopicStatus.Deleted)
            return Result.Failure("Cannot update deleted topic");

        Content = newContent;
        MarkAsUpdated();
        return Result.Success();
    }

    public void Pin(Guid moderatorId)
    {
        IsPinned = true;
        MarkAsUpdated();
    }

    public void Unpin(Guid moderatorId)
    {
        IsPinned = false;
        MarkAsUpdated();
    }

    public Result Lock(Guid moderatorId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
            return Result.Failure("Lock reason is required");

        Status = TopicStatus.Locked;
        LockReason = reason.Trim();
        MarkAsUpdated();
        return Result.Success();
    }

    public void Unlock(Guid moderatorId)
    {
        Status = TopicStatus.Active;
        LockReason = null;
        MarkAsUpdated();
    }

    public void Archive(Guid moderatorId)
    {
        Status = TopicStatus.Archived;
        MarkAsUpdated();
    }

    public void Delete(Guid moderatorId)
    {
        Status = TopicStatus.Deleted;
        MarkAsUpdated();
    }

    public void IncrementViewCount()
    {
        ViewCount++;
        MarkAsUpdated();
    }

    public bool CanUserReply(Guid userId)
    {
        return Status == TopicStatus.Active;
    }

    public Reply? FindReply(Guid replyId)
    {
        return _replies.FirstOrDefault(r => r.Id == replyId);
    }
}