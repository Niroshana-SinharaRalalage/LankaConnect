using System.Diagnostics;
using LankaConnect.Modules.Forms.Domain;
using LankaConnect.Modules.Forms.Domain.Entities;
using LankaConnect.Modules.Forms.Domain.Enums;
using LankaConnect.Modules.Forms.Domain.DomainEvents;
using LankaConnect.Modules.Forms.Domain.Repositories;
using System.Security.Cryptography;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Common;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events.Repositories;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Modules.Forms.Application.Queries.GetMyFormResponse;

public class GetMyFormResponseQueryHandler : IQueryHandler<GetMyFormResponseQuery, FormResponseDto>
{
    private readonly IFormResponseRepository _formResponseRepository;
    private readonly ILogger<GetMyFormResponseQueryHandler> _logger;

    public GetMyFormResponseQueryHandler(
        IFormResponseRepository formResponseRepository,
        ILogger<GetMyFormResponseQueryHandler> logger)
    {
        _formResponseRepository = formResponseRepository;
        _logger = logger;
    }

    public async Task<Result<FormResponseDto>> Handle(GetMyFormResponseQuery request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetMyFormResponse"))
        using (LogContext.PushProperty("EntityType", "FormResponse"))
        using (LogContext.PushProperty("FormId", request.FormId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "GetMyFormResponse START: FormId={FormId}",
                request.FormId);

            try
            {
                var tokenHash = ComputeSha256Hash(request.AccessToken);
                var response = await _formResponseRepository.GetByAccessTokenHashAsync(tokenHash, cancellationToken);

                if (response == null || response.EventFormId != request.FormId)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "GetMyFormResponse FAILED: Response not found or token mismatch - FormId={FormId}, Duration={ElapsedMs}ms",
                        request.FormId, stopwatch.ElapsedMilliseconds);
                    return Result<FormResponseDto>.Failure("Response not found or invalid access token");
                }

                var dto = MapToDto(response);

                stopwatch.Stop();

                _logger.LogInformation(
                    "GetMyFormResponse COMPLETE: ResponseId={ResponseId}, FormId={FormId}, Duration={ElapsedMs}ms",
                    response.Id, request.FormId, stopwatch.ElapsedMilliseconds);

                return Result<FormResponseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetMyFormResponse FAILED: Exception - FormId={FormId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.FormId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }

    private static FormResponseDto MapToDto(Modules.Forms.Domain.Entities.FormResponse response)
    {
        return new FormResponseDto
        {
            Id = response.Id,
            EventFormId = response.EventFormId,
            RespondentName = response.RespondentName,
            RespondentEmail = response.RespondentEmail,
            RespondentUserId = response.RespondentUserId,
            SubmittedAt = response.SubmittedAt,
            Answers = response.Answers.Select(a => new FormAnswerDto
            {
                Id = a.Id,
                FormQuestionId = a.FormQuestionId,
                QuestionTextSnapshot = a.QuestionTextSnapshot,
                TextValue = a.TextValue,
                SelectedOptionIds = a.SelectedOptionIds.ToList(),
                SelectedOptionTextSnapshots = a.SelectedOptionTextSnapshots.ToList(),
                BooleanValue = a.BooleanValue
            }).ToList()
        };
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(input);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
