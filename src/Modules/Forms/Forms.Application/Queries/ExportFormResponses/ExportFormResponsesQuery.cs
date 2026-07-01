using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Contracts;

namespace LankaConnect.Modules.Forms.Application.Queries.ExportFormResponses;

/// <summary>
/// Query to export custom form responses to CSV or Excel format.
/// Phase 6A.110: Form response export functionality
/// </summary>
public record ExportFormResponsesQuery(
    Guid EventId,
    Guid FormId,
    ExportFormat Format
) : IQuery<ExportResult>;