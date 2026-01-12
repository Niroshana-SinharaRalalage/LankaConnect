using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Handler for CreateNewsletterCommand
/// Phase 6A.74: Newsletter/News Alert creation with location targeting
/// </summary>
public class CreateNewsletterCommandHandler : ICommandHandler<CreateNewsletterCommand, Guid>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<CreateNewsletterCommandHandler> _logger;

    public CreateNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        IUnitOfWork _unitOfWork,
        ICurrentUserService currentUserService,
        ILogger<CreateNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        this._unitOfWork = _unitOfWork;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateNewsletterCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("[Newsletter] Creating newsletter: {Title}", request.Title);

            // Create value objects
            var titleResult = NewsletterTitle.Create(request.Title);
            if (titleResult.IsFailure)
            {
                _logger.LogWarning("[Newsletter] Invalid title: {Error}", titleResult.Error);
                return Result<Guid>.Failure(titleResult.Error);
            }

            var descriptionResult = NewsletterDescription.Create(request.Description);
            if (descriptionResult.IsFailure)
            {
                _logger.LogWarning("[Newsletter] Invalid description: {Error}", descriptionResult.Error);
                return Result<Guid>.Failure(descriptionResult.Error);
            }

            // Create newsletter aggregate
            var newsletterResult = Domain.Communications.Entities.Newsletter.Create(
                titleResult.Value,
                descriptionResult.Value,
                _currentUserService.UserId,
                request.EmailGroupIds ?? new List<Guid>(),
                request.IncludeNewsletterSubscribers,
                request.EventId,
                request.MetroAreaIds,
                request.TargetAllLocations);

            if (newsletterResult.IsFailure)
            {
                _logger.LogWarning("[Newsletter] Failed to create newsletter: {Error}", newsletterResult.Error);
                return Result<Guid>.Failure(newsletterResult.Error);
            }

            var newsletter = newsletterResult.Value;

            // Save to repository
            await _newsletterRepository.AddAsync(newsletter, cancellationToken);
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation("[Newsletter] Newsletter created successfully: {NewsletterID}", newsletter.Id);

            return Result<Guid>.Success(newsletter.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Newsletter] Error creating newsletter: {Title}", request.Title);
            return Result<Guid>.Failure("An error occurred while creating the newsletter");
        }
    }
}
