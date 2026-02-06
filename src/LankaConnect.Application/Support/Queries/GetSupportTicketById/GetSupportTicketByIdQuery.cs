using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Support.DTOs;

namespace LankaConnect.Application.Support.Queries.GetSupportTicketById;

/// <summary>
/// Query to get detailed support ticket by ID for admin view
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record GetSupportTicketByIdQuery(Guid TicketId) : IQuery<SupportTicketDetailsDto>;
