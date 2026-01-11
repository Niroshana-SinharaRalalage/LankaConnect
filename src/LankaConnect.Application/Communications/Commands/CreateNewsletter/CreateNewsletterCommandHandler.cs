using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Users.Enums;
using Microsoft.Extensions.Logging;
using MediatR;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Handler for CreateNewsletterCommand
/// Phase 6A.74: Creates a new newsletter in Draft status
///
/// Authorization: Requires EventOrganizer, Admin, or AdminManager role
/// Business Rules:
/// - Must have at least one recipient source (email groups OR newsletter subscribers)
/// - Title max 200 characters
/// - Description max 5000 characters
/// </summary>
public class CreateNewsletterCommandHandler : IRequestHandler<CreateNewsletterCommand, Result<Guid>>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateNewsletterCommandHandler> _logger;

    public CreateNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        ICurrentUserService currentUserService,
        IUnitOfWork unitOfWork,
        ILogger<CreateNewsletterCommandHandler> _logger)
    {
        _newsletterRepository = newsletterRepository;
        _currentUserService = currentUserService;
        _unitOfWork = unitOfWork;
        this._logger = _logger;
    }

    public async Task<Result<Guid>> Handle(CreateNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "[Newsletter] CreateNewsletterCommand START - Title: {Title}, EmailGroups: {EmailGroupCount}, IncludeSubscribers: {IncludeSubscribers}, EventId: {EventId}",
                request.Title, request.EmailGroupIds.Count, request.IncludeNewsletterSubscribers, request.EventId);

            // 1. Validate user is authenticated
            var userId = _currentUserService.UserId;
            if (userId == Guid.Empty)
            {
                _logger.LogWarning("[Newsletter] CreateNewsletterCommand FAILED - User not authenticated");
                return Result<Guid>.Failure("User must be authenticated to create a newsletter");
            }

            // 2. Validate user has permission (EventOrganizer, Admin, or AdminManager)
            var userRole = _currentUserService.UserRole;
            if (userRole != UserRole.EventOrganizer &&
                userRole != UserRole.EventOrganizerAndBusinessOwner &&
                userRole != UserRole.Admin &&
                userRole != UserRole.AdminManager)
            {
                _logger.LogWarning(
                    "[Newsletter] CreateNewsletterCommand FAILED - Unauthorized role: {Role} for user {UserId}",
                    userRole, userId);
                return Result<Guid>.Failure("Only Event Organizers, Admins, and Admin Managers can create newsletters");
            }

            // 3. Create value objects with validation
            var titleResult = NewsletterTitle.Create(request.Title);
            if (!titleResult.IsSuccess)
            {
                _logger.LogWarning(
                    "[Newsletter] CreateNewsletterCommand FAILED - Invalid title: {Error}",
                    titleResult.Error);
                return Result<Guid>.Failure(titleResult.Error);
            }

            var descriptionResult = NewsletterDescription.Create(request.Description);
            if (!descriptionResult.IsSuccess)
            {
                _logger.LogWarning(
                    "[Newsletter] CreateNewsletterCommand FAILED - Invalid description: {Error}",
                    descriptionResult.Error);
                return Result<Guid>.Failure(descriptionResult.Error);
            }

            // 4. Create newsletter via domain factory method (validates business rules)
            var createResult = Newsletter.Create(
                titleResult.Value,
                descriptionResult.Value,
                userId,
                request.EmailGroupIds,
                request.IncludeNewsletterSubscribers,
                request.EventId);

            if (!createResult.IsSuccess)
            {
                _logger.LogWarning(
                    "[Newsletter] CreateNewsletterCommand FAILED - Domain validation error: {Errors}",
                    string.Join(", ", createResult.Errors));
                return Result<Guid>.Failure(createResult.Errors);
            }

            var newsletter = createResult.Value;

            _logger.LogInformation(
                "[Newsletter] Newsletter domain entity created - Id: {NewsletterId}, CreatedBy: {UserId}",
                newsletter.Id, userId);

            // 5. Save to repository
            await _newsletterRepository.AddAsync(newsletter, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Newsletter] CreateNewsletterCommand SUCCESS - Id: {NewsletterId}, Title: {Title}",
                newsletter.Id, request.Title);

            return Result<Guid>.Success(newsletter.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Newsletter] CreateNewsletterCommand EXCEPTION - Title: {Title}, UserId: {UserId}",
                request.Title, _currentUserService.UserId);
            throw; // Let global exception handler catch this
        }
    }
}
