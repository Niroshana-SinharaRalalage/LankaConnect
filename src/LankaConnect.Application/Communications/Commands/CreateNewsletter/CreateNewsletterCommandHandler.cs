using System.Diagnostics;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Communications;
using LankaConnect.Domain.Communications.Entities;
using LankaConnect.Domain.Communications.ValueObjects;
using LankaConnect.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Communications.Commands.CreateNewsletter;

/// <summary>
/// Phase 6A.74: Handler for creating newsletters
/// Creates newsletter in Draft status with email groups and location targeting
/// Phase 6A.X Observability: Enhanced with comprehensive structured logging
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
        using (LogContext.PushProperty("Operation", "CreateNewsletter"))
        using (LogContext.PushProperty("EntityType", "Newsletter"))
        using (LogContext.PushProperty("UserId", _currentUserService.UserId))
        using (LogContext.PushProperty("Title", request.Title))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateNewsletter START: User={UserId}, Title={Title}, IsAnnouncementOnly={IsAnnouncementOnly}, EmailGroupCount={EmailGroupCount}, MetroAreaCount={MetroAreaCount}",
                _currentUserService.UserId,
                request.Title,
                request.IsAnnouncementOnly,
                request.EmailGroupIds?.Count ?? 0,
                request.MetroAreaIds?.Count ?? 0);

            try
            {
                // Validation: User must be authenticated
                if (!_currentUserService.IsAuthenticated)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateNewsletter FAILED: User not authenticated - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure("You must be authenticated to create newsletters");
                }

                // Create value objects
                _logger.LogInformation("CreateNewsletter: Creating value objects - Title={Title}", request.Title);

                var titleResult = NewsletterTitle.Create(request.Title);
                if (titleResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateNewsletter FAILED: Invalid title - Error={Error}, Duration={ElapsedMs}ms",
                        titleResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(titleResult.Error);
                }

                var descriptionResult = NewsletterDescription.Create(request.Description);
                if (descriptionResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateNewsletter FAILED: Invalid description - Error={Error}, Duration={ElapsedMs}ms",
                        descriptionResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(descriptionResult.Error);
                }

                // Validate and load email groups
                if (request.EmailGroupIds != null && request.EmailGroupIds.Any())
                {
                    var distinctGroupIds = request.EmailGroupIds.Distinct().ToList();

                    _logger.LogInformation(
                        "CreateNewsletter: Validating email groups - GroupCount={GroupCount}",
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
                                "CreateNewsletter FAILED: Email group not found - GroupId={GroupId}, Duration={ElapsedMs}ms",
                                groupId,
                                stopwatch.ElapsedMilliseconds);
                            return Result<Guid>.Failure($"Email group with ID {groupId} not found");
                        }

                        if (emailGroup.OwnerId != _currentUserService.UserId && !_currentUserService.IsAdmin)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "CreateNewsletter FAILED: User does not have permission to use email group - GroupId={GroupId}, GroupName={GroupName}, OwnerId={OwnerId}, Duration={ElapsedMs}ms",
                                groupId,
                                emailGroup.Name,
                                emailGroup.OwnerId,
                                stopwatch.ElapsedMilliseconds);
                            return Result<Guid>.Failure($"You do not have permission to use email group '{emailGroup.Name}'");
                        }

                        if (!emailGroup.IsActive)
                        {
                            stopwatch.Stop();
                            _logger.LogWarning(
                                "CreateNewsletter FAILED: Email group is inactive - GroupId={GroupId}, GroupName={GroupName}, Duration={ElapsedMs}ms",
                                groupId,
                                emailGroup.Name,
                                stopwatch.ElapsedMilliseconds);
                            return Result<Guid>.Failure($"Email group '{emailGroup.Name}' is inactive and cannot be used");
                        }
                    }

                    _logger.LogInformation(
                        "CreateNewsletter: All email groups validated successfully - GroupCount={GroupCount}",
                        distinctGroupIds.Count);
                }

                // Create Newsletter aggregate
                // Phase 6A.74 Part 14: Pass IsAnnouncementOnly flag to determine newsletter type
                _logger.LogInformation(
                    "CreateNewsletter: Creating newsletter aggregate - IsAnnouncementOnly={IsAnnouncementOnly}, IncludeNewsletterSubscribers={IncludeNewsletterSubscribers}, TargetAllLocations={TargetAllLocations}",
                    request.IsAnnouncementOnly,
                    request.IncludeNewsletterSubscribers,
                    request.TargetAllLocations);

                // Phase 6A.85: CRITICAL BUG FIX - Populate ALL metro areas when targetAllLocations = true
                // Root Cause: Newsletter.MetroAreaIds must contain all metros for matching logic to work:
                //   Newsletter.MetroAreaIds ∩ Subscriber.MetroAreaIds = Matched Recipients
                // When targetAllLocations = true but MetroAreaIds is empty, NO matches occur → 0 emails sent
                IEnumerable<Guid>? metroAreaIds = request.MetroAreaIds;

                if (request.TargetAllLocations && (metroAreaIds == null || !metroAreaIds.Any()))
                {
                    _logger.LogInformation(
                        "[Phase 6A.85] Newsletter targets all locations - querying all active metro areas from database");

                    try
                    {
                        var dbContext = _dbContext as DbContext
                            ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

                        var allMetroAreaIds = await dbContext.Set<MetroArea>()
                            .Where(m => m.IsActive)
                            .Select(m => m.Id)
                            .ToListAsync(cancellationToken);

                        metroAreaIds = allMetroAreaIds;

                        _logger.LogInformation(
                            "[Phase 6A.85] Successfully populated {Count} metro areas for target_all_locations newsletter",
                            allMetroAreaIds.Count);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        _logger.LogError(ex,
                            "[Phase 6A.85] FAILED to query metro areas for target_all_locations newsletter - Duration={ElapsedMs}ms",
                            stopwatch.ElapsedMilliseconds);
                        return Result<Guid>.Failure("Failed to load metro areas for newsletter targeting all locations");
                    }
                }
                else if (request.TargetAllLocations && metroAreaIds != null && metroAreaIds.Any())
                {
                    // Edge case: targetAllLocations=true but user somehow provided specific metros
                    // Log warning but use the database query (targetAllLocations is authoritative)
                    _logger.LogWarning(
                        "[Phase 6A.85] Newsletter has targetAllLocations=true AND specific metro IDs provided ({Count}). " +
                        "This is unexpected. Re-querying all active metros as targetAllLocations is authoritative.",
                        metroAreaIds.Count());

                    try
                    {
                        var dbContext = _dbContext as DbContext
                            ?? throw new InvalidOperationException("DbContext must be EF Core DbContext");

                        var allMetroAreaIds = await dbContext.Set<MetroArea>()
                            .Where(m => m.IsActive)
                            .Select(m => m.Id)
                            .ToListAsync(cancellationToken);

                        metroAreaIds = allMetroAreaIds;

                        _logger.LogInformation(
                            "[Phase 6A.85] Replaced {ProvidedCount} provided metros with {AllCount} active metros",
                            request.MetroAreaIds?.Count() ?? 0,
                            allMetroAreaIds.Count);
                    }
                    catch (Exception ex)
                    {
                        stopwatch.Stop();
                        _logger.LogError(ex,
                            "[Phase 6A.85] FAILED to query metro areas - Duration={ElapsedMs}ms",
                            stopwatch.ElapsedMilliseconds);
                        return Result<Guid>.Failure("Failed to load metro areas for newsletter");
                    }
                }
                else
                {
                    _logger.LogInformation(
                        "[Phase 6A.85] Newsletter targets specific locations - using provided {Count} metro area(s)",
                        metroAreaIds?.Count() ?? 0);
                }

                var newsletterResult = Newsletter.Create(
                    titleResult.Value,
                    descriptionResult.Value,
                    _currentUserService.UserId,
                    request.EmailGroupIds ?? new List<Guid>(),
                    request.IncludeNewsletterSubscribers,
                    request.EventId,
                    metroAreaIds,  // Phase 6A.85: Use populated metros (not request.MetroAreaIds)
                    request.TargetAllLocations,
                    request.IsAnnouncementOnly);  // Phase 6A.74 Part 14: Pass announcement-only flag

                if (newsletterResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateNewsletter FAILED: Newsletter creation failed - Error={Error}, Duration={ElapsedMs}ms",
                        newsletterResult.Error,
                        stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(newsletterResult.Error);
                }

                using (LogContext.PushProperty("NewsletterId", newsletterResult.Value.Id))
                {
                    _logger.LogInformation(
                        "CreateNewsletter: Newsletter aggregate created successfully - NewsletterId={NewsletterId}, Status={Status}",
                        newsletterResult.Value.Id,
                        newsletterResult.Value.Status);

                    // Add to repository
                    await _newsletterRepository.AddAsync(newsletterResult.Value, cancellationToken);

                    // Commit changes
                    await _unitOfWork.CommitAsync(cancellationToken);

                    stopwatch.Stop();
                    _logger.LogInformation(
                        "CreateNewsletter COMPLETE: NewsletterId={NewsletterId}, User={UserId}, IsAnnouncementOnly={IsAnnouncementOnly}, Status={Status}, EmailGroupCount={EmailGroupCount}, MetroAreaCount={MetroAreaCount}, Duration={ElapsedMs}ms",
                        newsletterResult.Value.Id,
                        _currentUserService.UserId,
                        request.IsAnnouncementOnly,
                        newsletterResult.Value.Status,
                        request.EmailGroupIds?.Count ?? 0,
                        request.MetroAreaIds?.Count ?? 0,
                        stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Success(newsletterResult.Value.Id);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "CreateNewsletter FAILED: Unexpected error - User={UserId}, Title={Title}, Duration={ElapsedMs}ms, ErrorMessage={ErrorMessage}",
                    _currentUserService.UserId,
                    request.Title,
                    stopwatch.ElapsedMilliseconds,
                    ex.Message);
                throw;
            }
        }
    }
}
