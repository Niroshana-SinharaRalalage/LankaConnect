using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Community;
using LankaConnect.Domain.Community.Enums;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class ForumTopicRepository : Repository<ForumTopic>, IForumTopicRepository
{
    public ForumTopicRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByForumAsync(Guid forumId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.ForumId == forumId)
            .OrderByDescending(t => t.IsPinned)
            .ThenByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.AuthorId == authorId)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByCategoryAsync(ForumCategory category, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Category == category && t.Status == TopicStatus.Active)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> GetByStatusAsync(TopicStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Status == status)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> GetPinnedTopicsAsync(Guid forumId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(t => t.ForumId == forumId && t.IsPinned && t.Status == TopicStatus.Active)
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<ForumTopic?> GetWithRepliesAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(t => t.Replies)
            .FirstOrDefaultAsync(t => t.Id == topicId, cancellationToken);
    }

    public async Task<IReadOnlyList<ForumTopic>> SearchAsync(string searchTerm, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return new List<ForumTopic>();

        var normalizedSearchTerm = searchTerm.Trim().ToLower();

        return await _dbSet
            .AsNoTracking()
            .Where(t => t.Status == TopicStatus.Active &&
                       (t.Title.Value.ToLower().Contains(normalizedSearchTerm) ||
                        t.Content.Value.ToLower().Contains(normalizedSearchTerm)))
            .OrderByDescending(t => t.UpdatedAt)
            .ToListAsync(cancellationToken);
    }
}