using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Domain.Enums;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Queries.GetPublicFormResponses;

/// <summary>
/// Phase 6A.146 — handler for the public form-responses read path.
///
/// Defense-in-depth: every "denied" branch returns the SAME 404 result with the
/// SAME generic "Form not found" message so callers cannot distinguish "doesn't
/// exist" from "exists but visibility is off" from "exists but is in
/// Draft/Archived". This is intentional — leaking the existence of the toggle
/// would let attackers fingerprint forms that organizers have decided to keep
/// private. The controller maps <see cref="ErrorKind.NotFound"/> to HTTP 404.
///
/// PII is redacted by construction via <see cref="PublicFormResponseDto"/>
/// (no name/email/userId properties). Reflection assertions in the test
/// fixture guard against future regression.
/// </summary>
public class GetPublicFormResponsesQueryHandler : IQueryHandler<GetPublicFormResponsesQuery, PublicFormResponsesDto>
{
    private const string NotFoundMessage = "Form not found";

    private readonly IFormRepository _eventFormRepository;
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ILogger<GetPublicFormResponsesQueryHandler> _logger;

    public GetPublicFormResponsesQueryHandler(
        IFormRepository eventFormRepository,
        IFormResponseRepository formResponseRepository,
        ILogger<GetPublicFormResponsesQueryHandler> logger)
    {
        _eventFormRepository = eventFormRepository;
        _formResponseRepository = formResponseRepository;
        _logger = logger;
    }

    public async Task<Result<PublicFormResponsesDto>> Handle(
        GetPublicFormResponsesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetPublicFormResponses"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", request.FormId))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();
            _logger.LogInformation(
                "GetPublicFormResponses START: FormId={FormId}, EventId={EventId}",
                request.FormId, request.EventId);

            try
            {
                var form = await _eventFormRepository.GetByIdAsync(request.FormId, cancellationToken);

                // Gate 1: form not found.
                if (form == null)
                {
                    _logger.LogInformation(
                        "GetPublicFormResponses DENIED (404): form not found - FormId={FormId}",
                        request.FormId);
                    return Result<PublicFormResponsesDto>.NotFound(NotFoundMessage);
                }

                // Gate 2: form exists but belongs to a different event.
                if (form.EventId != request.EventId)
                {
                    _logger.LogInformation(
                        "GetPublicFormResponses DENIED (404): form belongs to different event - FormId={FormId}, RequestEventId={RequestEventId}, FormEventId={FormEventId}",
                        request.FormId, request.EventId, form.EventId);
                    return Result<PublicFormResponsesDto>.NotFound(NotFoundMessage);
                }

                // Gate 3: organizer hasn't enabled public visibility. Return 404
                // (NOT 403) to avoid leaking the existence of the toggle state.
                if (!form.AllowAttendeesToViewResponses)
                {
                    _logger.LogInformation(
                        "GetPublicFormResponses DENIED (404): visibility flag off - FormId={FormId}",
                        request.FormId);
                    return Result<PublicFormResponsesDto>.NotFound(NotFoundMessage);
                }

                // Gate 4: form must be in a status that makes sense for public viewing.
                // Architect-locked: Active + Closed both publish (Closed is a
                // historical record); Draft + Archived are private.
                if (form.Status != FormStatus.Active && form.Status != FormStatus.Closed)
                {
                    _logger.LogInformation(
                        "GetPublicFormResponses DENIED (404): form status not eligible - FormId={FormId}, Status={Status}",
                        request.FormId, form.Status);
                    return Result<PublicFormResponsesDto>.NotFound(NotFoundMessage);
                }

                // All gates cleared — fetch responses. v1 paginates with int.MaxValue
                // page size to return everything in one round-trip; if any form
                // exceeds ~300 responses in staging the architect flagged adding
                // proper pagination as a follow-up.
                var (responses, totalCount) = await _formResponseRepository.GetPaginatedAsync(
                    request.FormId, page: 1, pageSize: int.MaxValue, cancellationToken);

                // Order by SubmittedAt ASC so the ordinal labels are stable AND
                // line up with the human-intuitive "first to submit" ordering.
                var ordered = responses.OrderBy(r => r.SubmittedAt).ToList();

                var publicResponses = ordered.Select((r, index) => new PublicFormResponseDto
                {
                    Id = r.Id,
                    // 2026-05-15 product correction: surface the respondent's name when
                    // provided. UI falls back to RespondentLabel when null. Email + UserId
                    // remain off the wire entirely (DTO doesn't carry those fields).
                    RespondentName = r.RespondentName,
                    RespondentLabel = $"Respondent {index + 1}",  // 1-based fallback
                    SubmittedOn = DateOnly.FromDateTime(r.SubmittedAt),
                    Answers = r.Answers
                        .OrderBy(a => a.Id)  // stable but deterministic order
                        .Select(a => new PublicFormAnswerDto
                        {
                            QuestionId = a.FormQuestionId,
                            QuestionTextSnapshot = a.QuestionTextSnapshot,
                            TextValue = a.TextValue,
                            SelectedOptionTextSnapshots = a.SelectedOptionTextSnapshots.ToList(),
                            BooleanValue = a.BooleanValue,
                        })
                        .ToList()
                }).ToList();

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetPublicFormResponses COMPLETE: FormId={FormId}, ReturnedCount={ReturnedCount}, TotalCount={TotalCount}, Duration={ElapsedMs}ms",
                    request.FormId, publicResponses.Count, totalCount, stopwatch.ElapsedMilliseconds);

                return Result<PublicFormResponsesDto>.Success(new PublicFormResponsesDto
                {
                    FormId = form.Id,
                    FormTitle = form.Title,
                    TotalCount = totalCount,
                    Responses = publicResponses
                });
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetPublicFormResponses FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
