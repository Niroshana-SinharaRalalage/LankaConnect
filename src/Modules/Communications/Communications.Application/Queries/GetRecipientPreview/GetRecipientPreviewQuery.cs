using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Contracts.LegacyPromotions; // 4C.h Day 5
namespace LankaConnect.Modules.Communications.Application.Queries.GetRecipientPreview;

/// <summary>
/// Query to preview newsletter recipients before sending
/// Phase 6A.74: Recipient preview with location targeting
/// </summary>
public record GetRecipientPreviewQuery(Guid NewsletterId) : IQuery<RecipientPreviewDto>;
