using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Phase 6A.74: Handler for creating newsletters
/// Creates newsletter in Draft status with email groups and location targeting
/// </summary>
public class CreateNewsletterCommandHandler : ICommandHandler<CreateNewsletterCommand, Guid>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateNewsletterCommandHandler> _logger;

    public CreateNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        IEmailGroupRepository emailGroupRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<CreateNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _emailGroupRepository = emailGroupRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateNewsletterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74] CreateNewsletterCommandHandler STARTED - User {UserId}, Title: {Title}",
            _currentUserService.UserId, request.Title);

        try
        {
            // Validate user is authenticated
            if (!_currentUserService.IsAuthenticated)
                return Result<Guid>.Failure("You must be authenticated to create newsletters");

            // Create value objects
            var titleResult = NewsletterTitle.Create(request.Title);
            if (titleResult.IsFailure)
                return Result<Guid>.Failure(titleResult.Error);

            var descriptionResult = NewsletterDescription.Create(request.Description);
            if (descriptionResult.IsFailure)
                return Result<Guid>.Failure(descriptionResult.Error);

            // Validate and load email groups
            if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
            {
                var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

                var dbContext = _dbContext as DbContext
                    ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

                var emailGroups = await dbContext.Set<EmailGroup>()
                    .Where(g => distinctGroupIds.Contains(g.Id))
                    .ToListAsync(cancellationToken);

                // Validate all groups exist, belong to current user, and are active
                foreach (var groupId in distinctGroupIds)
                {
                    var emailGroup = emailGroups.FirstOrDefault(g => g.Id == groupId);

                    if (emailGroup == null)
                        return Result<Guid>.Failure($"Email group with ID {groupId} not found");

                    if (emailGroup.OwnerId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                        return Result<Guid>.Failure($"You do not have permission to use email group '{emailGroup.Name}'");

                    if (!emailGroup.IsActive)
                        return Result<Guid>.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
                }
            }

            // Create Newsletter aggregate
            var newsletterResult = Newsletter.Create(
                titleResult.Value,
                descriptionResult.Value,
                _currentUserService.UserId,
                request.EmailGroupIds ?? new List<Guid>(),
                request.IncludeNewsletterSubscribers,
                request.EventId,
                request.MetroAreaIds,
                request.TargetAllLocations);

            if (newsletterResult.IsFailure)
                return Result<Guid>.Failure(newsletterResult.Error);

            // Add to repository
            await _newsletterRepository.AddAsync(newsletterResult.Value, cancellationToken);

            // Commit changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.74] Newsletter created successfully - ID: {NewsletterId}, User: {UserId}",
                newsletterResult.Value.Id, _currentUserService.UserId);

            return Result<Guid>.Success(newsletterResult.Value.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74] ERROR creating newsletter - User: {UserId}, Title: {Title}",
                _currentUserService.UserId, request.Title);
            throw;
        }
    }
}
