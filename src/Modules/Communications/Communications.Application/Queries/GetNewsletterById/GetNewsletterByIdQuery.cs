using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
namespace LankaConnect.Modules.Communications.Application.Queries.GetNewsletterById;

/// <summary>
/// Query to get a newsletter by ID
/// Phase 6A.74: Newsletter retrieval
/// </summary>
public record GetNewsletterByIdQuery(Guid Id) : IQuery<NewsletterDto>;
