using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain.Community.Enums;
namespace LankaConnect.Modules.Communications.Domain.Community;

public interface IForumTopicRepository : IRepository<ForumTopic>
{
    Task<IReadOnlyList<ForumTopic>> GetByForumAsync(Guid forumId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForumTopic>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForumTopic>> GetByCategoryAsync(ForumCategory category, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForumTopic>> GetByStatusAsync(TopicStatus status, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForumTopic>> GetPinnedTopicsAsync(Guid forumId, CancellationToken cancellationToken = default);
    Task<ForumTopic?> GetWithRepliesAsync(Guid topicId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ForumTopic>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default);
}
