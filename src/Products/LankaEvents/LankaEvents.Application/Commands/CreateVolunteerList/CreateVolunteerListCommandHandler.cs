using System.Diagnostics;
using LankaConnect.BuildingBlocks.Application.Common.Interfaces;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Products.LankaEvents.Domain;
using LankaConnect.Modules.Identity.Domain.DomainEvents;
using LankaConnect.Products.LankaEvents.Domain.Entities;
using LankaConnect.Products.LankaEvents.Infrastructure.Data; // Wave 8.5.g direct-SaveChanges
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Serilog.Context;
namespace LankaConnect.Products.LankaEvents.Application.Commands.CreateVolunteerList;

/// <summary>
/// Phase 7D.1: Handler for the role-oriented volunteer-list command. Delegates to
/// SignUpList.CreateVolunteerList which enforces Kind=Volunteers, slot-only items,
/// HasOpenItems=false — no parallel aggregate (architect Option A').
/// </summary>
public class CreateVolunteerListCommandHandler : ICommandHandler<CreateVolunteerListCommand, Guid>
{
    private readonly IEventRepository _eventRepository;
    private readonly LankaEventsDbContext _dbContext; // Wave 8.5.g direct-SaveChanges
    private readonly ILogger<CreateVolunteerListCommandHandler> _logger;

    public CreateVolunteerListCommandHandler(
        IEventRepository eventRepository,
        LankaEventsDbContext dbContext,
        ILogger<CreateVolunteerListCommandHandler> logger)
    {
        _eventRepository = eventRepository;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<Result<Guid>> Handle(CreateVolunteerListCommand request, CancellationToken cancellationToken)
    {
        using (LogContext.PushProperty("Operation", "CreateVolunteerList"))
        using (LogContext.PushProperty("EntityType", "SignUpList"))
        using (LogContext.PushProperty("EventId", request.EventId))
        {
            var stopwatch = Stopwatch.StartNew();

            _logger.LogInformation(
                "CreateVolunteerList START: EventId={EventId}, Category={Category}, RoleCount={RoleCount}",
                request.EventId, request.Category, request.Roles?.Count ?? 0);

            try
            {
                var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
                if (@event == null)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateVolunteerList FAILED: Event not found - EventId={EventId}, Duration={ElapsedMs}ms",
                        request.EventId, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure($"Event with ID {request.EventId} not found");
                }

                var roles = (request.Roles ?? new List<VolunteerRoleDto>()).Select(r => (
                    roleName: r.RoleName,
                    volunteersNeeded: r.VolunteersNeeded,
                    suggestedPerSlot: r.SuggestedPerSlot,
                    notes: r.Notes
                ));

                var signUpListResult = SignUpList.CreateVolunteerList(
                    request.Category,
                    request.Description,
                    roles);

                if (signUpListResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateVolunteerList FAILED: Domain validation failed - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, signUpListResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(signUpListResult.Error);
                }

                var addResult = @event.AddSignUpList(signUpListResult.Value);
                if (addResult.IsFailure)
                {
                    stopwatch.Stop();
                    _logger.LogWarning(
                        "CreateVolunteerList FAILED: Event.AddSignUpList rejected - EventId={EventId}, Error={Error}, Duration={ElapsedMs}ms",
                        request.EventId, addResult.Error, stopwatch.ElapsedMilliseconds);
                    return Result<Guid>.Failure(addResult.Error);
                }

                await _dbContext.SaveChangesAsync(cancellationToken);

                stopwatch.Stop();
                _logger.LogInformation(
                    "CreateVolunteerList COMPLETE: EventId={EventId}, SignUpListId={SignUpListId}, RoleCount={RoleCount}, Duration={ElapsedMs}ms",
                    request.EventId, signUpListResult.Value.Id, request.Roles?.Count ?? 0, stopwatch.ElapsedMilliseconds);

                return Result<Guid>.Success(signUpListResult.Value.Id);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex,
                    "CreateVolunteerList FAILED: Exception - EventId={EventId}, Duration={ElapsedMs}ms, Error={ErrorMessage}",
                    request.EventId, stopwatch.ElapsedMilliseconds, ex.Message);
                throw;
            }
        }
    }
}
