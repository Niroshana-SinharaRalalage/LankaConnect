using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Products.LankaEvents.Contracts;

namespace LankaConnect.Modules.Forms.Application.Queries.GetPublicFormResponses;

/// <summary>
/// Phase 6A.146 — public, [AllowAnonymous] query that returns the PII-redacted
/// list of all responses for a form. The handler enforces three defense-in-depth
/// gates (form-not-found / wrong-event / visibility-flag-off / Draft-or-Archived
/// status) and projects through <see cref="PublicFormResponseDto"/> whose shape
/// physically excludes RespondentName / RespondentEmail / RespondentUserId.
/// </summary>
public record GetPublicFormResponsesQuery(
    Guid EventId,
    Guid FormId
) : IQuery<PublicFormResponsesDto>;
