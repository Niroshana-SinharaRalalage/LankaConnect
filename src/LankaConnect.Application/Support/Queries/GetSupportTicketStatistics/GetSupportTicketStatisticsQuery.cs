using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Support.DTOs;

namespace LankaConnect.Application.Support.Queries.GetSupportTicketStatistics;

/// <summary>
/// Query to get support ticket statistics for admin dashboard
/// Phase 6A.90: Support/Feedback System
/// </summary>
public record GetSupportTicketStatisticsQuery : IQuery<SupportTicketStatisticsDto>;
