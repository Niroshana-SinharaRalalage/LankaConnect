using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LankaConnect.Application.Communications.Commands.UpdateNewsletter;

/// <summary>
/// Phase 6A.74: Handler for updating newsletters
/// Only Draft newsletters can be updated
/// </summary>
public class UpdateNewsletterCommandHandler : ICommandHandler<UpdateNewsletterCommand, bool>
{
    private readonly INewsletterRepository _newsletterRepository;
    private readonly IEmailGroupRepository _emailGroupRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IApplicationDbContext _dbContext;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateNewsletterCommandHandler> _logger;

    public UpdateNewsletterCommandHandler(
        INewsletterRepository newsletterRepository,
        IEmailGroupRepository emailGroupRepository,
        ICurrentUserService currentUserService,
        IApplicationDbContext dbContext,
        IUnitOfWork unitOfWork,
        ILogger<UpdateNewsletterCommandHandler> logger)
    {
        _newsletterRepository = newsletterRepository;
        _emailGroupRepository = emailGroupRepository;
        _currentUserService = currentUserService;
        _dbContext = dbContext;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(UpdateNewsletterCommand request, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "[Phase 6A.74] UpdateNewsletterCommandHandler STARTED - Newsletter {NewsletterId}, User {UserId}",
            request.Id, _currentUserService.UserId);

        try
        {
            // Retrieve newsletter
            var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
            if (newsletter == null)
                return Result<bool>.Failure("Newsletter not found");

            // Authorization: Only creator or admin can update
            if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                return Result<bool>.Failure("You do not have permission to update this newsletter");

            // Create value objects
            var titleResult = NewsletterTitle.Create(request.Title);
            if (titleResult.IsFailure)
                return Result<bool>.Failure(titleResult.Error);

            var descriptionResult = NewsletterDescription.Create(request.Description);
            if (descriptionResult.IsFailure)
                return Result<bool>.Failure(descriptionResult.Error);

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
                        return Result<bool>.Failure($"Email group with ID {groupId} not found");

                    if (emailGroup.OwnerId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                        return Result<bool>.Failure($"You do not have permission to use email group '{emailGroup.Name}'");

                    if (!emailGroup.IsActive)
                        return Result<bool>.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
                }
            }

            // Update newsletter
            var updateResult = newsletter.Update(
                titleResult.Value,
                descriptionResult.Value,
                request.EmailGroupIds ?? new List<Guid>(),
                request.IncludeNewsletterSubscribers,
                request.EventId,
                request.MetroAreaIds,
                request.TargetAllLocations);

            if (updateResult.IsFailure)
                return Result<bool>.Failure(updateResult.Error);

            // Phase 6A.74 HOTFIX: Sync shadow navigation for email groups and metro areas
            // The domain's Update() method updates _emailGroupIds and _metroAreaIds lists,
            // but EF Core only tracks shadow navigation (_emailGroupEntities, _metroAreaEntities).
            // We must sync the loaded entities to shadow navigation for persistence to junction tables.
            // Pattern mirrors NewsletterRepository.AddAsync (lines 39-90)

            var dbContext2 = _dbContext as DbContext
                ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

            // Sync email groups shadow navigation
            if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
            {
                var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

                var emailGroupEntities = await dbContext2.Set<EmailGroup>()
                    .Where(eg => distinctGroupIds.Contains(eg.Id))
                    .ToListAsync(cancellationToken);

                var emailGroupsCollection = dbContext2.Entry(newsletter).Collection("_emailGroupEntities");
                emailGroupsCollection.CurrentValue = emailGroupEntities;

                _logger.LogInformation("[Phase 6A.74 HOTFIX] Synced {Count} email groups to shadow navigation",
                    emailGroupEntities.Count);
            }
            else
            {
                // Clear email groups if none provided
                var emailGroupsCollection = dbContext2.Entry(newsletter).Collection("_emailGroupEntities");
                emailGroupsCollection.CurrentValue = new List<EmailGroup>();

                _logger.LogInformation("[Phase 6A.74 HOTFIX] Cleared email groups shadow navigation");
            }

            // Sync metro areas shadow navigation
            if (request.MetroAreaIds != null && request.MetroAreaIds.Any())
            {
                var distinctMetroIds = request.MetroAreaIds.Distinct().ToList();

                var metroAreaEntities = await dbContext2.Set<Domain.Events.MetroArea>()
                    .Where(m => distinctMetroIds.Contains(m.Id))
                    .ToListAsync(cancellationToken);

                var metroAreasCollection = dbContext2.Entry(newsletter).Collection("_metroAreaEntities");
                metroAreasCollection.CurrentValue = metroAreaEntities;

                _logger.LogInformation("[Phase 6A.74 HOTFIX] Synced {Count} metro areas to shadow navigation",
                    metroAreaEntities.Count);
            }
            else
            {
                // Clear metro areas if none provided
                var metroAreasCollection = dbContext2.Entry(newsletter).Collection("_metroAreaEntities");
                metroAreasCollection.CurrentValue = new List<Domain.Events.MetroArea>();

                _logger.LogInformation("[Phase 6A.74 HOTFIX] Cleared metro areas shadow navigation");
            }

            // Commit changes
            await _unitOfWork.CommitAsync(cancellationToken);

            _logger.LogInformation(
                "[Phase 6A.74] Newsletter updated successfully - ID: {NewsletterId}, User: {UserId}",
                request.Id, _currentUserService.UserId);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "[Phase 6A.74] ERROR updating newsletter - Newsletter {NewsletterId}, User: {UserId}",
                request.Id, _currentUserService.UserId);
            throw;
        }
    }
}
