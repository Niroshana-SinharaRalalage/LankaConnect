using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
namespace LankaConnect.Modules.Communications.Application.Queries.GetNewslettersByCreator;

/// <summary>
/// Query to get newsletters created by current user
/// Phase 6A.74: Newsletter listing
/// </summary>
public record GetNewslettersByCreatorQuery : IQuery<List<NewsletterDto>>;
