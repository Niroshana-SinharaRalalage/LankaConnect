using LankaConnect.Modules.Identity.Contracts; // W4.6.a: ICurrentUserService moved here
using System.Diagnostics;
using System.Text.RegularExpressions;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Communications.Queries.GetEmailTemplateById;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Modules.Communications.Application.Commands.UpdateEmailTemplate;

/// <summary>
/// Phase 6A.89: Handler for UpdateEmailTemplateCommand
/// Updates email template content with audit logging
/// </summary>
public class UpdateEmailTemplateCommandHandler : IRequestHandler<UpdateEmailTemplateCommand, Result<EmailTemplateDetailDto>>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateEmailTemplateCommandHandler> _logger;

    public UpdateEmailTemplateCommandHandler(
        IEmailTemplateRepository emailTemplateRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<UpdateEmailTemplateCommandHandler> logger)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<EmailTemplateDetailDto>> Handle(
        UpdateEmailTemplateCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "UpdateEmailTemplate"))
        using (LogContext.PushProperty("EntityType", "EmailTemplate"))
        using (LogContext.PushProperty("TemplateId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateEmailTemplate START: TemplateId={TemplateId}, User={UserId}",
                request.Id,
                _currentUserService.UserId);

            try
            {
                // Validate user is admin
                if (!_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailTemplate FAILED: User not admin - UserId={UserId}, Duration={ElapsedMs}ms",
                        _currentUserService.UserId,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure("Only administrators can update email templates");
                }

                // Get the template
                var template = await _emailTemplateRepository.GetByIdAsync(request.Id, cancellationToken);
                if (template == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailTemplate FAILED: Template not found - TemplateId={TemplateId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure("Email template not found");
                }

                _logger.LogInformation(
                    "UpdateEmailTemplate: Template found - TemplateId={TemplateId}, Name={TemplateName}, CurrentSubject={Subject}",
                    template.Id,
                    template.Name,
                    template.SubjectTemplate.Value);

                // Create subject value object
                var subjectResult = EmailSubject.Create(request.Subject);
                if (!subjectResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailTemplate FAILED: Invalid subject - TemplateId={TemplateId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.Id,
                        subjectResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure(subjectResult.Error ?? "Invalid subject");
                }

                // Update template via domain method
                var updateResult = template.UpdateTemplate(
                    subjectResult.Value!,
                    request.TextTemplate,
                    request.HtmlTemplate);

                if (!updateResult.IsSuccess)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateEmailTemplate FAILED: Domain validation - TemplateId={TemplateId}, Errors={Errors}, Duration={ElapsedMs}ms",
                        request.Id,
                        string.Join("; ", updateResult.Errors),
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure(updateResult.Errors);
                }

                // Update optional fields if provided
                if (request.Tags != null)
                {
                    template.SetTags(request.Tags);
                }

                // Save changes
                _emailTemplateRepository.Update(template);
                await _unitOfWork.CommitAsync(cancellationToken);

                // Extract parameters for response
                var parameters = ExtractParameters(template.TextTemplate, template.HtmlTemplate);

                // Map to response DTO
                var dto = new EmailTemplateDetailDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    Subject = template.SubjectTemplate.Value,
                    TextTemplate = template.TextTemplate,
                    HtmlTemplate = template.HtmlTemplate,
                    Category = template.Category.Value,
                    Type = template.Type.ToString(),
                    IsActive = template.IsActive,
                    Tags = template.Tags,
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt,
                    RequiredParameters = parameters.Required,
                    OptionalParameters = parameters.Optional
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "UpdateEmailTemplate COMPLETE: TemplateId={TemplateId}, Name={TemplateName}, User={UserId}, Duration={ElapsedMs}ms",
                    template.Id,
                    template.Name,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds);

                return Result<EmailTemplateDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateEmailTemplate FAILED: Exception - TemplateId={TemplateId}, User={UserId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }

    private static (List<string> Required, List<string> Optional) ExtractParameters(
        string textTemplate,
        string? htmlTemplate)
    {
        var allContent = textTemplate + (htmlTemplate ?? string.Empty);
        var pattern = @"\{\{?(\w+)\}?\}";
        var matches = Regex.Matches(allContent, pattern);

        var parameters = matches
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();

        var commonRequired = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "recipientName", "recipientEmail", "userName", "userEmail",
            "eventName", "eventTitle", "businessName"
        };

        var required = parameters.Where(p => commonRequired.Contains(p)).ToList();
        var optional = parameters.Where(p => !commonRequired.Contains(p)).ToList();

        return (required, optional);
    }
}

/// <summary>
/// Phase 6A.89: Handler for ToggleEmailTemplateActiveCommand
/// </summary>
public class ToggleEmailTemplateActiveCommandHandler : IRequestHandler<ToggleEmailTemplateActiveCommand, Result<EmailTemplateDetailDto>>
{
    private readonly IEmailTemplateRepository _emailTemplateRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ToggleEmailTemplateActiveCommandHandler> _logger;

    public ToggleEmailTemplateActiveCommandHandler(
        IEmailTemplateRepository emailTemplateRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<ToggleEmailTemplateActiveCommandHandler> logger)
    {
        _emailTemplateRepository = emailTemplateRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<EmailTemplateDetailDto>> Handle(
        ToggleEmailTemplateActiveCommand request,
        CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "ToggleEmailTemplateActive"))
        using (LogContext.PushProperty("TemplateId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "ToggleEmailTemplateActive START: TemplateId={TemplateId}, IsActive={IsActive}, User={UserId}",
                request.Id,
                request.IsActive,
                _currentUserService.UserId);

            try
            {
                if (!_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ToggleEmailTemplateActive FAILED: User not admin - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure("Only administrators can toggle template status");
                }

                var template = await _emailTemplateRepository.GetByIdAsync(request.Id, cancellationToken);
                if (template == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "ToggleEmailTemplateActive FAILED: Template not found - TemplateId={TemplateId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<EmailTemplateDetailDto>.Failure("Email template not found");
                }

                template.SetActive(request.IsActive);
                _emailTemplateRepository.Update(template);
                await _unitOfWork.CommitAsync(cancellationToken);

                var dto = new EmailTemplateDetailDto
                {
                    Id = template.Id,
                    Name = template.Name,
                    Description = template.Description,
                    Subject = template.SubjectTemplate.Value,
                    TextTemplate = template.TextTemplate,
                    HtmlTemplate = template.HtmlTemplate,
                    Category = template.Category.Value,
                    Type = template.Type.ToString(),
                    IsActive = template.IsActive,
                    Tags = template.Tags,
                    CreatedAt = template.CreatedAt,
                    UpdatedAt = template.UpdatedAt,
                    RequiredParameters = new List<string>(),
                    OptionalParameters = new List<string>()
                };

                stopwatch.Stop();
                _logger.LogInformation(
                    "ToggleEmailTemplateActive COMPLETE: TemplateId={TemplateId}, IsActive={IsActive}, Duration={ElapsedMs}ms",
                    template.Id,
                    template.IsActive,
                    stopwatch.ElapsedMilliseconds);

                return Result<EmailTemplateDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "ToggleEmailTemplateActive FAILED: Exception - TemplateId={TemplateId}, Duration={ElapsedMs}ms",
                    request.Id,
                    stopwatch.ElapsedMilliseconds);
                throw;
            }
        }
    }
}
