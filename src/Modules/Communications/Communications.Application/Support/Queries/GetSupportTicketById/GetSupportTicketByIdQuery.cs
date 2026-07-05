using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Modules.Communications.Application.Support.DTOs;
namespace LankaConnect.Modules.Communications.Application.Support.Queries.GetSupportTicketById;

/// <summary>
/// Query to get detailed support ticket by ID for admin view
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record GetSupportTicketByIdQuery(Guid TicketId) : IQuery<SupportTicketDetailsDto>;
