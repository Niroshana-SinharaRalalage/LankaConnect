using LankaConnect.Domain.Common;

namespace LankaConnect.Domain.Community;

public interface IReplyRepository : IRepository<Reply>
{
    Task<IReadOnlyList<Reply>> GetByTopicAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reply>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reply>> GetByParentReplyAsync(Guid parentReplyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reply>> GetSolutionsForTopicAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Reply>> GetTopHelpfulRepliesAsync(Guid topicId, int count, CancellationToken cancellationToken = default);
}