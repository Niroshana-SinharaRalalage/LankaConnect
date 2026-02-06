using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.UpdateNewsletter;

/// <summary>
/// Phase 6A.74: Handler for updating newsletters
/// Only Draft newsletters can be updated
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "UpdateNewsletter"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("NewsletterId", request.Id))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "UpdateNewsletter START: NewsletterId={NewsletterId}, User={UserId}, Title={Title}, EmailGroupCount={EmailGroupCount}, MetroAreaCount={MetroAreaCount}",
                request.Id,
                _currentUserService.UserId,
                request.Title,
                request.EmailGroupIds?.Count ?? 0,
                request.MetroAreaIds?.Count ?? 0);

            try
            {
                // Validation: Newsletter ID is required
                if (request.Id == Guid.Empty)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateNewsletter FAILED: Newsletter ID is required - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter ID is required");
                }

                // Retrieve newsletter
                var newsletter = await _newsletterRepository.GetByIdAsync(request.Id, cancellationToken);
                if (newsletter == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateNewsletter FAILED: Newsletter not found - NewsletterId={NewsletterId}, Duration={ElapsedMs}ms",
                        request.Id,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("Newsletter not found");
                }

                _logger.LogInformation(
                    "UpdateNewsletter: Newsletter found - NewsletterId={NewsletterId}, Status={Status}, CreatedBy={CreatedByUserId}",
                    newsletter.Id,
                    newsletter.Status,
                    newsletter.CreatedByUserId);

                // Authorization: Only creator or admin can update
                if (newsletter.CreatedByUserId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateNewsletter FAILED: User does not have permission - NewsletterId={NewsletterId}, UserId={UserId}, CreatedBy={CreatedByUserId}, IsAdmin={IsAdmin}, Duration={ElapsedMs}ms",
                        request.Id,
                        _currentUserService.UserId,
                        newsletter.CreatedByUserId,
                        _currentUserService.IsAdmin,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure("You do not have permission to update this newsletter");
                }

                // Create value objects
                _logger.LogInformation("UpdateNewsletter: Creating value objects - Title={Title}", request.Title);

                var titleResult = NewsletterTitle.Create(request.Title);
                if (titleResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateNewsletter FAILED: Invalid title - Error={Error}, Duration={ElapsedMs}ms",
                        titleResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure(titleResult.Error);
                }

                var descriptionResult = NewsletterDescription.Create(request.Description);
                if (descriptionResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateNewsletter FAILED: Invalid description - Error={Error}, Duration={ElapsedMs}ms",
                        descriptionResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure(descriptionResult.Error);
                }

                // Validate and load email groups
                if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
                {
                    var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

                    _logger.LogInformation(
                        "UpdateNewsletter: Validating email groups - GroupCount={GroupCount}",
                        distinctGroupIds.Count);

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
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "UpdateNewsletter FAILED: Email group not found - GroupId={GroupId}, Duration={ElapsedMs}ms",
                                groupId,
                                stopwatch.ElapsedMilliseconds);
                            return Result<bool>.Failure($"Email group with ID {groupId} not found");
                        }

                        if (emailGroup.OwnerId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "UpdateNewsletter FAILED: User does not have permission to use email group - GroupId={GroupId}, GroupName={GroupName}, OwnerId={OwnerId}, Duration={ElapsedMs}ms",
                                groupId,
                                emailGroup.Name,
                                emailGroup.OwnerId,
                                stopwatch.ElapsedMilliseconds);
                            return Result<bool>.Failure($"You do not have permission to use email group '{emailGroup.Name}'");
                        }

                        if (!emailGroup.IsActive)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "UpdateNewsletter FAILED: Email group is inactive - GroupId={GroupId}, GroupName={GroupName}, Duration={ElapsedMs}ms",
                                groupId,
                                emailGroup.Name,
                                stopwatch.ElapsedMilliseconds);
                            return Result<bool>.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
                        }
                    }

                    _logger.LogInformation(
                        "UpdateNewsletter: All email groups validated successfully - GroupCount={GroupCount}",
                        distinctGroupIds.Count);
                }

                // Update newsletter
                _logger.LogInformation(
                    "UpdateNewsletter: Updating newsletter aggregate - IncludeNewsletterSubscribers={IncludeNewsletterSubscribers}, TargetAllLocations={TargetAllLocations}",
                    request.IncludeNewsletterSubscribers,
                    request.TargetAllLocations);

                var updateResult = newsletter.Update(
                    titleResult.Value,
                    descriptionResult.Value,
                    request.EmailGroupIds ?? new List<Guid>(),
                    request.IncludeNewsletterSubscribers,
                    request.EventId,
                    request.MetroAreaIds,
                    request.TargetAllLocations);

                if (updateResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "UpdateNewsletter FAILED: Newsletter update failed - Error={Error}, Duration={ElapsedMs}ms",
                        updateResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<bool>.Failure(updateResult.Error);
                }

                _logger.LogInformation(
                    "UpdateNewsletter: Newsletter domain model updated successfully - NewsletterId={NewsletterId}",
                    request.Id);

                // Phase 6A.74 HOTFIX: Sync shadow navigation for email groups and metro areas
                // The domain's Update() method updates _emailGroupIds and _metroAreaIds lists,
                // but EF Core only tracks shadow navigation (_emailGroupEntities, _metroAreaEntities).
                // We must sync the loaded entities to shadow navigation for persistence to junction tables.
                // Pattern mirrors NewsletterRepository.AddAsync (lines 39-90)
                _logger.LogInformation(
                    "UpdateNewsletter: [HOTFIX] Syncing shadow navigation for EF Core many-to-many relationships - NewsletterId={NewsletterId}",
                    request.Id);

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

                    _logger.LogInformation(
                        "UpdateNewsletter: [HOTFIX] Synced {Count} email groups to shadow navigation",
                        emailGroupEntities.Count);
                }
                else
                {
                    // Clear email groups if none provided
                    var emailGroupsCollection = dbContext2.Entry(newsletter).Collection("_emailGroupEntities");
                    emailGroupsCollection.CurrentValue = new List<EmailGroup>();

                    _logger.LogInformation("UpdateNewsletter: [HOTFIX] Cleared email groups shadow navigation");
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

                    _logger.LogInformation(
                        "UpdateNewsletter: [HOTFIX] Synced {Count} metro areas to shadow navigation",
                        metroAreaEntities.Count);
                }
                else
                {
                    // Clear metro areas if none provided
                    var metroAreasCollection = dbContext2.Entry(newsletter).Collection("_metroAreaEntities");
                    metroAreasCollection.CurrentValue = new List<Domain.Events.MetroArea>();

                    _logger.LogInformation("UpdateNewsletter: [HOTFIX] Cleared metro areas shadow navigation");
                }

                // Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "UpdateNewsletter COMPLETE: NewsletterId={NewsletterId}, User={UserId}, EmailGroupCount={EmailGroupCount}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                    request.Id,
                    _currentUserService.UserId,
                    request.EmailGroupIds?.Count ?? 0,
                    request.MetroAreaIds?.Count ?? 0,
                    stopwatch.ElapsedMilliseconds);

                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "UpdateNewsletter FAILED: Unexpected error - NewsletterId={NewsletterId}, User={UserId}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    request.Id,
                    _currentUserService.UserId,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}
