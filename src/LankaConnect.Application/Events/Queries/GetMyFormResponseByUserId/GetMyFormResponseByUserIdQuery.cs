using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;

namespace LankaConnect.Application.Events.Queries.GetMyFormResponseByUserId;

/// <summary>
/// Gets a logged-in user's own response by userId (for authenticated users).
/// Phase 6A.106-110 Fix: Allow logged-in users to see Edit/Delete buttons in Signup Forms tab.
/// </summary>
public record GetMyFormResponseByUserIdQuery(Guid FormId, Guid UserId) : IQuery<FormResponseDto?>;
