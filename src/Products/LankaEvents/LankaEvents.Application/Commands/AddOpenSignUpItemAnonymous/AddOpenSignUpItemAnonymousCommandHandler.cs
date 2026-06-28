using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Products.LankaEvents.Application.Queries.CheckEventRegistration;
using LankaConnect.Domain.Common;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace LankaConnect.Products.LankaEvents.Application.Commands.AddOpenSignUpItemAnonymous;

/// <summary>
/// Handler for adding Open sign-up items for anonymous users
/// Phase 6A.44: Original — gated on member-account + event-registration.
/// Phase 6A.140: Gates removed. Any email may add an Open item. Smart UserId resolution:
///   - Email matches a LankaConnect member → item recorded under that member's real UserId
///     (they can later log in and manage it).
///   - Email does not match a member → item recorded under the deterministic anonymous GUID
///     (same as the prior anonymous path).
/// </summary>
public class AddOpenSignUpItemAnonymousCommandHandler : ICommandHandler<AddOpenSignUpItemAnonymousCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddOpenSignUpItemAnonymousCommandHandler> _logger;
    private readonly ILogger<CheckEventRegistrationQueryHandler> _checkEventRegistrationLogger;

    public AddOpenSignUpItemAnonymousCommandHandler(
        IEventRepository eventRepository,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork,
        ILogger<AddOpenSignUpItemAnonymousCommandHandler> logger,
        ILogger<CheckEventRegistrationQueryHandler> checkEventRegistrationLogger)
    {
        _eventRepository = eventRepository;
        _context = context;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _checkEventRegistrationLogger = checkEventRegistrationLogger;
    }

    public async Task<Result<Guid>> Handle(AddOpenSignUpItemAnonymousCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "AddOpenSignUpItemAnonymous"))
        using (LogContext.PushProperty("EntityType", "SignUpItem"))
        using (LogContext.PushProperty("EventId", request.EventId))
        using (LogContext.PushProperty("SignUpListId", request.SignUpListId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "AddOpenSignUpItemAnonymous START: EventId={EventId}, SignUpListId={SignUpListId}, ContactEmail={ContactEmail}, ItemName={ItemName}",
                request.EventId, request.SignUpListId, request.ContactEmail, request.ItemName);

            try
            {
                // Validate email format
                if (string.IsNullOrWhiteSpace(request.ContactEmail))
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItemAnonymous FAILED: Email validation failed - Duration={ElapsedMs}ms",
                        stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure("Email is required");
                }

                var emailToCheck = request.ContactEmail.Trim();

                _logger.LogInformation(
                    "AddOpenSignUpItemAnonymous: Email validated - Email={Email}",
                    emailToCheck);

                // Phase 6A.140: Smart UserId resolution. Same pattern as
                // CommitToSignUpItemAnonymousCommandHandler — gates removed, real-UserId-when-
                // member resolution + deterministic-GUID fallback.
                var checkQuery = new CheckEventRegistrationQuery(request.EventId, emailToCheck);
                var checkHandler = new CheckEventRegistrationQueryHandler(_context, _checkEventRegistrationLogger);
                var registrationResult = await checkHandler.Handle(checkQuery, cancellationToken);

                if (registrationResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItemAnonymous FAILED: Registration check failed - EventId={EventId}, Email={Email}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, emailToCheck, registrationResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(registrationResult.Error);
                }

                var check = registrationResult.Value;
                var resolvedUserId = check.HasUserAccount && check.UserId.HasValue
                    ? check.UserId.Value
                    : GenerateDeterministicGuid(emailToCheck);

                _logger.LogInformation(
                    "AddOpenSignUpItemAnonymous: Smart-resolved UserId - EventId={EventId}, HasUserAccount={HasUserAccount}, IsRegistered={IsRegistered}, ResolvedUserId={ResolvedUserId}",
                    request.EventId, check.HasUserAccount, check.IsRegisteredForEvent, resolvedUserId);

                // Step 4: Get the event with sign-up lists
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItemAnonymous FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Event with ID {request.EventId} not found");
                }

                _logger.LogInformation(
                    "AddOpenSignUpItemAnonymous: Event loaded - EventId={EventId}, Title={Title}",
                    @event.Id, @event.Title.Value);

                // Step 5: Get the sign-up list
                var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
                if (signUpList == null)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItemAnonymous FAILED: Sign-up list not found - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure($"Sign-up list with ID {request.SignUpListId} not found");
                }

                _logger.LogInformation(
                    "AddOpenSignUpItemAnonymous: Sign-up list loaded - SignUpListId={SignUpListId}, Category={Category}",
                    signUpList.Id, signUpList.Category);

                // Phase 6A.140: resolvedUserId set above by smart resolution.

                // Step 7: Add the Open item (domain method handles validation and auto-commitment)
                var itemResult = signUpList.AddOpenItem(
                    resolvedUserId,
                    request.ItemName,
                    request.Quantity,
                    request.Notes,
                    request.ContactName,
                    request.ContactEmail,
                    request.ContactPhone);

                if (itemResult.IsFailure)
                {
                    stopwatch.Stop();

                    _logger.LogWarning(
                        "AddOpenSignUpItemAnonymous FAILED: Domain validation failed - EventId={EventId}, SignUpListId={SignUpListId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, request.SignUpListId, itemResult.Error, stopwatch.ElapsedMilliseconds);

                    return Result<Guid>.Failure(itemResult.Error);
                }

                _logger.LogInformation(
                    "AddOpenSignUpItemAnonymous: Domain method succeeded - ItemId={ItemId}, ItemName={ItemName}",
                    itemResult.Value.Id, request.ItemName);

                // Step 8: Commit changes
                await _unitOfWork.CommitAsync(cancellationToken);

                stopwatch.Stop();

                _logger.LogInformation(
                    "AddOpenSignUpItemAnonymous COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, ItemId={ItemId}, Duration={ElapsedMs}ms",
                    request.EventId, request.SignUpListId, itemResult.Value.Id, stopwatch.ElapsedMilliseconds);

                // Return the created item ID
                return Result<Guid>.Success(itemResult.Value.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();

                _logger.LogError(ex,
                    "AddOpenSignUpItemAnonymous FAILED: Exception occurred - EventId={EventId}, SignUpListId={SignUpListId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, request.SignUpListId, stopwatch.ElapsedMilliseconds, ex.Message);

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
