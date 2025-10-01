using Microsoft.EntityFrameworkCore;
using LankaConnect.Domain.Community;

namespace LankaConnect.Infrastructure.Data.Repositories;

public class ReplyRepository : Repository<Reply>, IReplyRepository
{
    public ReplyRepository(AppDbContext context) : base(context)
    {
    }

    public async Task<IReadOnlyList<Reply>> GetByTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.TopicId == topicId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reply>> GetByAuthorAsync(Guid authorId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.AuthorId == authorId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reply>> GetByParentReplyAsync(Guid parentReplyId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.ParentReplyId == parentReplyId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reply>> GetSolutionsForTopicAsync(Guid topicId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.TopicId == topicId && r.IsMarkedAsSolution)
            .OrderByDescending(r => r.HelpfulVotes)
            .ThenBy(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reply>> GetTopHelpfulRepliesAsync(Guid topicId, int count, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .AsNoTracking()
            .Where(r => r.TopicId == topicId && r.HelpfulVotes > 0)
            .OrderByDescending(r => r.HelpfulVotes)
            .ThenBy(r => r.CreatedAt)
            .Take(count)
            .ToListAsync(cancellationToken);
    }
}