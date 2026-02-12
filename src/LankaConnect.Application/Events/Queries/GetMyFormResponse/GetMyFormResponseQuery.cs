using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetMyFormResponse;

/// <summary>
/// Gets a respondent's own response by access token (for anonymous edit page).
/// </summary>
public record GetMyFormResponseQuery(Guid FormId, string AccessToken) : IQuery<FormResponseDto>;
