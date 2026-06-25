using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.CheckEventRegistration;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using LankaConnect.Domain.Users.DomainEvents; // W4.7.a: user-aggregate events moved here
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CommitToSignUpItemAnonymous;

/// <summary>
/// Handler for anonymous sign-up item commitment
/// Phase 6A.23: Original — gated on member-account + event-registration.
/// Phase 6A.140: Gates removed. Any email may commit. Smart UserId resolution:
///   - Email matches a LankaConnect member → commitment uses that member's real UserId
///     (they can later log in and manage the commitment from their account).
///   - Email does not match a member → commitment uses the deterministic anonymous GUID
///     (same as the prior anonymous path).
/// </summary>
public class CommitToSignUpItemAnonymousCommandHandler : ICommandHandler<CommitToSignUpItemAnonymousCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CommitToSignUpItemAnonymousCommandHandler> _logger;
    private readonly ILogger<CheckEventRegistrationQueryHandler> _checkEventRegistrationLogger;

    public CommitToSignUpItemAnonymousCommandHandler(
        IEventRepository eventRepository,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<CommitToSignUpItemAnonymousCommandHandler> logger,
        ILogger<CheckEventRegistrationQueryHandler> checkEventRegistrationLogger)
    {
        _eventRepository = eventRepository;
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _checkEventRegistrationLogger = checkEventRegistrationLogger;
    }

    public async Task<Result<Guid>> Handle(CommitToSignUpItemAnonymousCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CommitToSignUpItemAnonymous"))
        using (LogContext.PushProperty("EntityType", "SignUpCommitment"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        using (LogContext.PushProperty("SignUpItemId", request.SignUpItemId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CommitToSignUpItemAnonymous START: EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, ContactEmail={ContactEmail}, Quantity={Quantity}",
                request.EventId, request.SignUpListId, request.SignUpItemId, request.ContactEmail, request.Quantity);

            try
            {
                // Validate email format
                if (string.IsNullOrWhiteSpace(request.ContactEmail))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Email validation failed - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Email is required");
                }

                var emailToCheck = request.ContactEmail.Trim();

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Email validated - Email={Email}",
                    emailToCheck);

                // Phase 6A.140: Smart UserId resolution.
                // Look the email up in Users. If a member row exists, the commitment is
                // bound to that member's real UserId (so they can later log in and Update /
                // Cancel from their account). If no member row exists, fall back to the
                // existing deterministic anonymous GUID. Both branches succeed — there is no
                // longer a "please log in" or "register for event first" rejection.
                // The CheckEventRegistrationQuery (Member + Registration lookup in one call)
                // is reused so we keep observability (member-status + registration-status are
                // still logged) without forking the lookup logic.
                var checkQuery = new CheckEventRegistrationQuery(request.EventId, emailToCheck);
                var checkHandler = new CheckEventRegistrationQueryHandler(_context, _checkEventRegistrationLogger);
                var registrationResult = await checkHandler.Handle(checkQuery, cancellationToken);

                if (registrationResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Registration check failed - EventId={EventId}, Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, emailToCheck, registrationResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(registrationResult.Error);
                }

                var check = registrationResult.Value;
                var resolvedUserId = check.HasUserAccount && check.UserId.HasValue
                    ? check.UserId.Value
                    : GenerateDeterministicGuid(emailToCheck);

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Smart-resolved UserId - EventId={EventId}, HasUserAccount={HasUserAccount}, IsRegistered={IsRegistered}, ResolvedUserId={ResolvedUserId}",
                    request.EventId, check.HasUserAccount, check.IsRegisteredForEvent, resolvedUserId);

                // Step 4: Get the event with sign-up lists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Step 5: Get the sign-up list
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Sign-up list loaded - SignUpListId={SignUpListId}, Category={Category}",
                    signUpList.Id, signUpList.Category);

                // Step 6: Get the sign-up item
                var signUpItem = signUpList.GetItem(request.SignUpItemId);
                if (signUpItem == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Sign-up item not found - EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, request.SignUpItemId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Sign-up item with ID {request.SignUpItemId} not found");
                }

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Sign-up item loaded - SignUpItemId={SignUpItemId}, ItemCategory={ItemCategory}",
                    signUpItem.Id, signUpItem.ItemCategory);

                // Phase 6A.140: resolvedUserId was set above (Step 1) based on whether the
                // email matched a real member or not. Step 7 was the old deterministic-GUID
                // assignment; it now lives inside the smart-resolution block.

                // Phase 6A.125: Determine effective quantities based on item type
                var physicalQuantity = request.PhysicalQuantity ?? request.Quantity;
                var slotsClaimed = request.SlotsClaimed;
                var itemTypeStr = signUpItem.ItemType.ToString();

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Sign-up item type={ItemType}, PhysicalQuantity={Qty}, SlotsClaimed={Slots}",
                    itemTypeStr, physicalQuantity, slotsClaimed);

                // Step 8: Check if user already has a commitment to this item
                var existingCommitment = signUpItem.Commitments.FirstOrDefault(c => c.UserId == resolvedUserId);

                Result commitResult;
                Guid commitmentId;

                if (existingCommitment != null)
                {
                    _logger.LogInformation(
                        "CommitToSignUpItemAnonymous: Updating existing commitment - CommitmentId={CommitmentId}, ItemType={ItemType}",
                        existingCommitment.Id, itemTypeStr);

                    if (signUpItem.ItemType == Domain.Events.Enums.SignUpItemType.Slot)
                    {
                        commitResult = signUpItem.UpdateSlotCommitment(
                            resolvedUserId,
                            slotsClaimed ?? request.Quantity,
                            request.Notes,
                            request.ContactName,
                            request.ContactEmail,
                            request.ContactPhone);
                    }
                    else
                    {
                        commitResult = signUpItem.UpdateCommitment(
                            resolvedUserId,
                            physicalQuantity,
                            request.Notes,
                            request.ContactName,
                            request.ContactEmail,
                            request.ContactPhone);
                    }
                    commitmentId = existingCommitment.Id;
                }
                else
                {
                    _logger.LogInformation(
                        "CommitToSignUpItemAnonymous: Adding new commitment - ItemType={ItemType}",
                        itemTypeStr);

                    if (signUpItem.ItemType == Domain.Events.Enums.SignUpItemType.Slot)
                    {
                        commitResult = signUpItem.AddSlotCommitment(
                            resolvedUserId,
                            slotsClaimed ?? request.Quantity,
                            request.Notes,
                            request.ContactName,
                            request.ContactEmail,
                            request.ContactPhone,
                            kind: signUpList.Kind);
                    }
                    else
                    {
                        commitResult = signUpItem.AddCommitment(
                            resolvedUserId,
                            physicalQuantity,
                            request.Notes,
                            request.ContactName,
                            request.ContactEmail,
                            request.ContactPhone,
                            kind: signUpList.Kind);
                    }

                    var newCommitment = signUpItem.Commitments.FirstOrDefault(c => c.UserId == resolvedUserId);
                    commitmentId = newCommitment?.Id ?? Guid.Empty;

                    _logger.LogInformation(
                        "CommitToSignUpItemAnonymous: New commitment created - CommitmentId={CommitmentId}",
                        commitmentId);
                }

                if (commitResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Commitment operation failed - SignUpItemId={SignUpItemId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.SignUpItemId, commitResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(commitResult.Error);
                }

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Commitment operation succeeded - CommitmentId={CommitmentId}",
                    commitmentId);

                // Step 9: Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, CommitmentId={CommitmentId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, request.SignUpItemId, commitmentId, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(commitmentId);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "CommitToSignUpItemAnonymous FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, SignUpItemId={SignUpItemId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, request.SignUpItemId, stopwatch.ElapsedMilliseconds, ex.Message);

                throw; // Re-throw to let MediatR/API handle
            }
        }
    }

    /// <summary>
    /// Generates a deterministic GUID from an email address
    /// Uses SHA256 hash and takes first 16 bytes to create a valid GUID
    /// Prefixed to avoid collisions with real user IDs
    /// </summary>
    private static Guid GenerateDeterministicGuid(string email)
    {
        var input = $"ANON_SIGNUP:{email}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        var guidBytes = new byte[16];
        Array.Copy(hash, guidBytes, 16);
        return new Guid(guidBytes);
    }
}
