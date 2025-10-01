using LankaConnect.Domain.Common;
using LankaConnect.Domain.Community.ValueObjects;

namespace LankaConnect.Domain.Community;

public class Reply : BaseEntity
{
    public Guid TopicId { get; private set; }
    public PostContent Content { get; private set; }
    public Guid AuthorId { get; private set; }
    public Guid? ParentReplyId { get; private set; }
    public int HelpfulVotes { get; private set; }
    public bool IsMarkedAsSolution { get; private set; }

    // EF Core constructor
    private Reply() 
    {
        Content = null!;
    }

    private Reply(Guid topicId, PostContent content, Guid authorId, Guid? parentReplyId = null)
    {
        TopicId = topicId;
        Content = content;
        AuthorId = authorId;
        ParentReplyId = parentReplyId;
        HelpfulVotes = 0;
        IsMarkedAsSolution = false;
    }

    public static Result<Reply> Create(Guid topicId, PostContent content, Guid authorId, Guid? parentReplyId = null)
    {
        if (topicId == Guid.Empty)
            return Result<Reply>.Failure("Topic ID is required");

        if (content == null)
            return Result<Reply>.Failure("Content is required");

        if (authorId == Guid.Empty)
            return Result<Reply>.Failure("Author ID is required");

        var reply = new Reply(topicId, content, authorId, parentReplyId);
        return Result<Reply>.Success(reply);
    }

    public Result UpdateContent(PostContent newContent, Guid editorId)
    {
        if (newContent == null)
            return Result.Failure("Content is required");

        if (editorId != AuthorId)
            return Result.Failure("Only the author can update the reply");

        Content = newContent;
        MarkAsUpdated();
        return Result.Success();
    }

    public void AddHelpfulVote()
    {
        HelpfulVotes++;
        MarkAsUpdated();
    }

    public void RemoveHelpfulVote()
    {
        if (HelpfulVotes > 0)
        {
            HelpfulVotes--;
            MarkAsUpdated();
        }
    }

    public void MarkAsSolution(Guid moderatorId)
    {
        IsMarkedAsSolution = true;
        MarkAsUpdated();
    }

    public void UnmarkAsSolution(Guid moderatorId)
    {
        IsMarkedAsSolution = false;
        MarkAsUpdated();
    }
}