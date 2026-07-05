using LankaConnect.BuildingBlocks.Application.Common;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Contracts;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Modules.Forms.Application.Queries.GetMyFormResponseByUserId;

/// <summary>
/// Handler for GetMyFormResponseByUserIdQuery.
/// Phase 6A.106-110 Fix: Enables logged-in users to see their form responses in Signup Forms tab.
/// Returns null if user has no response (not an error - used for conditional UI display).
/// </summary>
public class GetMyFormResponseByUserIdQueryHandler : IQueryHandler<GetMyFormResponseByUserIdQuery, FormResponseDto?>
{
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ILogger<GetMyFormResponseByUserIdQueryHandler> _logger;

    public GetMyFormResponseByUserIdQueryHandler(
        IFormResponseRepository formResponseRepository,
        ILogger<GetMyFormResponseByUserIdQueryHandler> logger)
    {
        _formResponseRepository = formResponseRepository;
        _logger = logger;
    }

    public async Task<Result<FormResponseDto?>> Handle(GetMyFormResponseByUserIdQuery request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "GetMyFormResponseByUserId START: FormId={FormId}, UserId={UserId}",
            request.FormId, request.UserId);

        // Get response for this form by this user (should be only one per form)
        var response = await _formResponseRepository.GetByFormAndUserAsync(request.FormId, request.UserId, cancellationToken);

        if (response == null)
        {
            _logger.LogInformation(
                "GetMyFormResponseByUserId COMPLETE: No response found - FormId={FormId}, UserId={UserId}",
                request.FormId, request.UserId);

            // Return Success with null (not an error - UI will show "Fill Out Form" button)
            return Result<FormResponseDto?>.Success(null);
        }

        _logger.LogInformation(
            "GetMyFormResponseByUserId COMPLETE: Found response - FormId={FormId}, UserId={UserId}, ResponseId={ResponseId}",
            request.FormId, request.UserId, response.Id);

        var dto = new FormResponseDto
        {
            Id = response.Id,
            EventFormId = response.EventFormId,
            RespondentName = response.RespondentName,
            RespondentEmail = response.RespondentEmail,
            SubmittedAt = response.SubmittedAt,
            Answers = response.Answers.Select(a => new FormAnswerDto
            {
                Id = a.Id,
                FormQuestionId = a.FormQuestionId,
                QuestionTextSnapshot = "", // Not loaded in this query
                TextValue = a.TextValue,
                BooleanValue = a.BooleanValue,
                SelectedOptionIds = a.SelectedOptionIds?.ToList() ?? new List<Guid>(),
                SelectedOptionTextSnapshots = a.SelectedOptionTextSnapshots?.ToList() ?? new List<string>()
            }).ToList()
        };

        return Result<FormResponseDto?>.Success(dto);
    }
}
