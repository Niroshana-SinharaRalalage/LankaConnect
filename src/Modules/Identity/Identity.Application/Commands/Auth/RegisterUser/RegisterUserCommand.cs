using MediatR;
using LankaConnect.Products.LankaEvents.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Modules.Identity.Domain.Enums;
namespace LankaConnect.Modules.Identity.Application.Commands.Auth.RegisterUser;

/// <summary>
/// Register User Command
/// Phase 6A.0: Added SelectedRole parameter for role-based registration
/// </summary>
public record RegisterUserCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    UserRole? SelectedRole = null, // Phase 6A.0: Optional role selection (defaults to GeneralUser)
    List<Guid>? PreferredMetroAreaIds = null, // Phase 5A: Optional metro areas
    string? WhatsAppPhoneNumber = null) : IRequest<Result<RegisterUserResponse>>; // Phase 7A.6A: Optional WhatsApp opt-in
