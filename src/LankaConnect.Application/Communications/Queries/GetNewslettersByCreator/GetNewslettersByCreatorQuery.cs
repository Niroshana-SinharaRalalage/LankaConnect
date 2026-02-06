using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;

namespace LankaConnect.Application.Communications.Queries.GetNewslettersByCreator;

/// <summary>
/// Query to get newsletters created by current user
/// Phase 6A.74: Newsletter listing
/// </summary>
public record GetNewslettersByCreatorQuery : IQuery<List<NewsletterDto>>;
