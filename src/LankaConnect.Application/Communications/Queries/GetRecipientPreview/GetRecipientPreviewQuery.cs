using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Common;

namespace LankaConnect.Application.Communications.Queries.GetRecipientPreview;

/// <summary>
/// Query to preview newsletter recipients before sending
/// Phase 6A.74: Recipient preview with location targeting
/// </summary>
public record GetRecipientPreviewQuery(Guid NewsletterId) : IQuery<RecipientPreviewDto>;
