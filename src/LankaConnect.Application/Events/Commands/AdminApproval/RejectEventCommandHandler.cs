using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Events;

namespace LankaConnect.Application.Events.Commands.AdminApproval;

public class RejectEventCommandHandler : ICommandHandler<RejectEventCommand>
{
    private readonly IEventRepository _eventRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RejectEventCommandHandler(IEventRepository eventRepository, IUnitOfWork unitOfWork)
    {
        _eventRepository = eventRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RejectEventCommand request, CancellationToken cancellationToken)
    {
        // Retrieve event
        var @event = await _eventRepository.GetByIdAsync(request.EventId, cancellationToken);
        if (@event == null)
            return Result.Failure("Event not found");

        // Use domain method to reject event
        var rejectResult = @event.Reject(request.RejectedByAdminId, request.Reason);
        if (rejectResult.IsFailure)
            return rejectResult;

        // Save changes (EF Core tracks changes automatically)
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}
