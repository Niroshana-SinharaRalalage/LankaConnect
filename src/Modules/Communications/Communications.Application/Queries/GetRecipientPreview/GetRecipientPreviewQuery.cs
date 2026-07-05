using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Common;
namespace LankaConnect.Modules.Communications.Application.Queries.GetRecipientPreview;

/// <summary>
/// Query to preview newsletter recipients before sending
/// Phase 6A.74: Recipient preview with location targeting
/// </summary>
public record GetRecipientPreviewQuery(Guid NewsletterId) : IQuery<RecipientPreviewDto>;
