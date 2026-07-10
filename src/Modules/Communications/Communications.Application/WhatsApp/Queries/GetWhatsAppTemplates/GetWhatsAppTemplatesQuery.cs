using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Communications.Domain;
using LankaConnect.Modules.Communications.Domain.Enums;
namespace LankaConnect.Modules.Communications.Application.WhatsApp.Queries.GetWhatsAppTemplates;

/// <summary>
/// Phase 7A.2: Query to get all registered WhatsApp templates.
/// </summary>
public record GetWhatsAppTemplatesQuery() : IQuery<IReadOnlyList<WhatsAppTemplateDto>>;

/// <summary>
/// DTO representing a WhatsApp message template.
/// </summary>
public class WhatsAppTemplateDto
{
    public Guid Id { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public WhatsAppTemplateCategory Category { get; init; }
    public WhatsAppTemplateStatus Status { get; init; }
    public int ParameterCount { get; init; }
    public string Language { get; init; } = "en";
    public bool IsApproved { get; init; }
    public bool IsUsable { get; init; }
    public string? HeaderText { get; init; }
    public string BodyText { get; init; } = string.Empty;
    public string? FooterText { get; init; }
    public List<string> ParameterNames { get; init; } = new();
}

/// <summary>
/// Phase 7A.2: Handler for GetWhatsAppTemplatesQuery.
/// Retrieves all templates from the repository and maps to DTOs.
/// </summary>
public class GetWhatsAppTemplatesQueryHandler : IRequestHandler<GetWhatsAppTemplatesQuery, Result<IReadOnlyList<WhatsAppTemplateDto>>>
{
    private readonly IWhatsAppTemplateRepository _templateRepository;
    private readonly ILogger<GetWhatsAppTemplatesQueryHandler> _logger;

    public GetWhatsAppTemplatesQueryHandler(
        IWhatsAppTemplateRepository templateRepository,
        ILogger<GetWhatsAppTemplatesQueryHandler> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<WhatsAppTemplateDto>>> Handle(
        GetWhatsAppTemplatesQuery request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "GetWhatsAppTemplates"))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation("GetWhatsAppTemplates START");

            try
            {
                var templates = await _templateRepository.GetAllTemplatesAsync(cancellationToken);

                var dtos = templates.Select(t => new WhatsAppTemplateDto
                {
                    Id = t.Id,
                    TemplateName = t.TemplateName,
                    DisplayName = t.DisplayName,
                    Category = t.Category,
                    Status = t.Status,
                    ParameterCount = t.ParameterCount,
                    Language = t.Language,
                    IsApproved = t.IsApproved,
                    IsUsable = t.IsUsable,
                    HeaderText = t.HeaderText,
                    BodyText = t.BodyText,
                    FooterText = t.FooterText,
                    ParameterNames = t.GetParameterNames()
                }).ToList() as IReadOnlyList<WhatsAppTemplateDto>;

                stopwatch.Stop();
                _logger.LogInformation(
                    "GetWhatsAppTemplates COMPLETE: TemplateCount={TemplateCount}, Duration={ElapsedMs}ms",
                    dtos.Count, stopwatch.ElapsedMilliseconds);

                return Result<IReadOnlyList<WhatsAppTemplateDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "GetWhatsAppTemplates FAILED: Unexpected error - Duration={ElapsedMs}ms",
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
