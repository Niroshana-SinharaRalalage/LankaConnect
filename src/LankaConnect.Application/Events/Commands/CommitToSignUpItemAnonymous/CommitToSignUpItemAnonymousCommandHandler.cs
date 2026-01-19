using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.CheckEventRegistration;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Application.Events.Commands.CommitToSignUpItemAnonymous;

/// <summary>
/// Handler for anonymous sign-up item commitment
/// Phase 6A.23: Supports anonymous sign-up workflow with proper UX flow
///
/// Flow:
/// 1. Check if email belongs to a member → Reject with "Please log in" message
/// 2. Check if registered for event → Reject with "Please register first" message
/// 3. If anonymous + registered → Allow commitment with deterministic UserId
/// </summary>
public class CommitToSignUpItemAnonymousCommandHandler : ICommandHandler<CommitToSignUpItemAnonymousCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CommitToSignUpItemAnonymousCommandHandler> _logger;

    public CommitToSignUpItemAnonymousCommandHandler(
        IEventRepository eventRepository,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<CommitToSignUpItemAnonymousCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
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

            _logger.LogDebug(
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

                // Step 1: Check registration status and member status
                var checkQuery = new CheckEventRegistrationQuery(request.EventId, emailToCheck);
                var checkHandler = new CheckEventRegistrationQueryHandler(_context);
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

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Registration check complete - ShouldPromptLogin={ShouldPromptLogin}, NeedsEventRegistration={NeedsEventRegistration}, CanCommitAnonymously={CanCommitAnonymously}",
                    check.ShouldPromptLogin, check.NeedsEventRegistration, check.CanCommitAnonymously);

                // Step 2: Validate based on UX flow
                if (check.ShouldPromptLogin)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Email belongs to member account - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        request.EventId, emailToCheck, stopwatch.ElapsedMilliseconds);

                    // Email belongs to a LankaConnect member - they should log in
                    return Result<Guid>.Failure("MEMBER_ACCOUNT:This email is associated with a LankaConnect account. Please log in to sign up for items.");
                }

                if (check.NeedsEventRegistration)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: User not registered for event - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        request.EventId, emailToCheck, stopwatch.ElapsedMilliseconds);

                    // Not registered for event
                    return Result<Guid>.Failure("NOT_REGISTERED:You must be registered for this event to sign up for items. Please register for the event first.");
                }

                // Step 3: User can proceed with anonymous commitment
                if (!check.CanCommitAnonymously)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "CommitToSignUpItemAnonymous FAILED: Cannot commit anonymously - EventId={EventId}, Email={Email}, Duration={ElapsedMs}ms",
                        request.EventId, emailToCheck, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Unable to process commitment. Please try again.");
                }

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

                // Step 7: Generate deterministic UserId for anonymous user
                // This ensures the same email always gets the same "virtual" UserId
                var anonymousUserId = GenerateDeterministicGuid(emailToCheck.ToLowerInvariant());

                _logger.LogInformation(
                    "CommitToSignUpItemAnonymous: Generated deterministic UserId - AnonymousUserId={AnonymousUserId}",
                    anonymousUserId);

                // Step 8: Check if user already has a commitment to this item
                var existingCommitment = signUpItem.Commitments.FirstOrDefault(c => c.UserId == anonymousUserId);

                Result commitResult;
                Guid commitmentId;

                if (existingCommitment != null)
                {
                    _logger.LogInformation(
                        "CommitToSignUpItemAnonymous: Updating existing commitment - CommitmentId={CommitmentId}, OldQuantity={OldQuantity}, NewQuantity={NewQuantity}",
                        existingCommitment.Id, existingCommitment.Quantity, request.Quantity);

                    // User already committed - update the existing commitment
                    commitResult = signUpItem.UpdateCommitment(
                        anonymousUserId,
                        request.Quantity,
                        request.Notes,
                        request.ContactName,
                        request.ContactEmail,
                        request.ContactPhone);
                    commitmentId = existingCommitment.Id;
                }
                else
                {
                    _logger.LogInformation(
                        "CommitToSignUpItemAnonymous: Adding new commitment - Quantity={Quantity}",
                        request.Quantity);

                    // New commitment - add it
                    commitResult = signUpItem.AddCommitment(
                        anonymousUserId,
                        request.Quantity,
                        request.Notes,
                        request.ContactName,
                        request.ContactEmail,
                        request.ContactPhone);

                    // Get the newly created commitment ID
                    var newCommitment = signUpItem.Commitments.FirstOrDefault(c => c.UserId == anonymousUserId);
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
