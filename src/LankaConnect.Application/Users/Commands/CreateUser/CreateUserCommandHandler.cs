using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Users;
using LankaConnect.Domain.Shared.ValueObjects;

namespace LankaConnect.Application.Users.Commands.CreateUser;

public class CreateUserCommandHandler : ICommandHandler<CreateUserCommand, Guid>
{
    private readonly LankaConnect.Domain.Users.IUserRepository _userRepository;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;

    public CreateUserCommandHandler(LankaConnect.Domain.Users.IUserRepository userRepository, LankaConnect.Domain.Common.IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Create Email value object
        var emailResult = LankaConnect.Domain.Shared.ValueObjects.Email.Create(request.Email);
        if (emailResult.IsFailure)
            return Result<Guid>.Failure(emailResult.Errors);

        // Check if user already exists
        var emailExists = await _userRepository.ExistsWithEmailAsync(emailResult.Value, cancellationToken);
        if (emailExists)
            return Result<Guid>.Failure("A user with this email already exists");

        // Create PhoneNumber value object if provided
        LankaConnect.Domain.Shared.ValueObjects.PhoneNumber? phoneNumber = null;
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            var phoneResult = LankaConnect.Domain.Shared.ValueObjects.PhoneNumber.Create(request.PhoneNumber);
            if (phoneResult.IsFailure)
                return Result<Guid>.Failure(phoneResult.Errors);
            phoneNumber = phoneResult.Value;
        }

        // Create User aggregate
        var userResult = User.Create(emailResult.Value, request.FirstName, request.LastName);
        if (userResult.IsFailure)
            return Result<Guid>.Failure(userResult.Errors);

        var user = userResult.Value;

        // Update profile if additional info provided
        if (phoneNumber != null || !string.IsNullOrEmpty(request.Bio))
        {
            var updateResult = user.UpdateProfile(
                request.FirstName,
                request.LastName,
                phoneNumber,
                request.Bio);

            if (updateResult.IsFailure)
                return Result<Guid>.Failure(updateResult.Errors);
        }

        // Save to repository
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(user.Id);
    }
}