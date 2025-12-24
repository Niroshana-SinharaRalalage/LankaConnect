using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Application.Events.Queries.CheckEventRegistration;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;
using System.Security.Cryptography;
using System.Text;

namespace LankaConnect.Application.Events.Commands.AddOpenSignUpItemAnonymous;

/// <summary>
/// Handler for adding Open sign-up items for anonymous users
/// Phase 6A.44: Supports anonymous Open item creation if user is registered for the event
///
/// Flow:
/// 1. Check if email belongs to a member → Reject with "Please log in" message
/// 2. Check if registered for event → Reject with "Please register first" message
/// 3. If anonymous + registered → Allow adding Open item with deterministic UserId
/// </summary>
public class AddOpenSignUpItemAnonymousCommandHandler : ICommandHandler<AddOpenSignUpItemAnonymousCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly IApplicationDbContext _context;
    private readonly IUnitOfWork _unitOfWork;

    public AddOpenSignUpItemAnonymousCommandHandler(
        IEventRepository eventRepository,
        IApplicationDbContext context,
        IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _context = context;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddOpenSignUpItemAnonymousCommand request, CancellationToken cancellationToken)
    {
        // Validate email format
        if (string.IsNullOrWhiteSpace(request.ContactEmail))
            return Result<Guid>.Failure("Email is required");

        var emailToCheck = request.ContactEmail.Trim();

        // Step 1: Check registration status and member status
        var checkQuery = new CheckEventRegistrationQuery(request.EventId, emailToCheck);
        var checkHandler = new CheckEventRegistrationQueryHandler(_context);
        var registrationResult = await checkHandler.Handle(checkQuery, cancellationToken);

        if (registrationResult.IsFailure)
            return Result<Guid>.Failure(registrationResult.Error);

        var check = registrationResult.Value;

        // Step 2: Validate based on UX flow
        if (check.ShouldPromptLogin)
        {
            // Email belongs to a LankaConnect member - they should log in
            return Result<Guid>.Failure("MEMBER_ACCOUNT:This email is associated with a LankaConnect account. Please log in to add items.");
        }

        if (check.NeedsEventRegistration)
        {
            // Not registered for event
            return Result<Guid>.Failure("NOT_REGISTERED:You must be registered for this event to add items. Please register for the event first.");
        }

        // Step 3: User can proceed with anonymous commitment
        if (!check.CanCommitAnonymously)
        {
            return Result<Guid>.Failure("Unable to process request. Please try again.");
        }

        // Step 4: Get the event with sign-up lists
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result<Guid>.Failure($"Event with ID {request.EventId} not found");

        // Step 5: Get the sign-up list
        var signUpList = @event.SignUpLists.FirstOrDefault(s => s.Id == request.SignUpListId);
        if (signUpList == null)
            return Result<Guid>.Failure($"Sign-up list with ID {request.SignUpListId} not found");

        // Step 6: Generate deterministic UserId for anonymous user
        // This ensures the same email always gets the same "virtual" UserId
        var anonymousUserId = GenerateDeterministicGuid(emailToCheck.ToLowerInvariant());

        // Step 7: Add the Open item (domain method handles validation and auto-commitment)
        var itemResult = signUpList.AddOpenItem(
            anonymousUserId,
            request.ItemName,
            request.Quantity,
            request.Notes,
            request.ContactName,
            request.ContactEmail,
            request.ContactPhone);

        if (itemResult.IsFailure)
            return Result<Guid>.Failure(itemResult.Error);

        // Step 8: Commit changes
        await _unitOfWork.CommitAsync(cancellationToken);

        // Return the created item ID
        return Result<Guid>.Success(itemResult.Value.Id);
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
