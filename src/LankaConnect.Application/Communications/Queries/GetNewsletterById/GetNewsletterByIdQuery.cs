using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;

namespace LankaConnect.Application.Communications.Queries.GetNewsletterById;

/// <summary>
/// Query to get a newsletter by ID
/// Phase 6A.74: Newsletter retrieval
/// </summary>
public record GetNewsletterByIdQuery(Guid Id) : IQuery<NewsletterDto>;
