using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Business.ValueObjects;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;
using DomainUnitOfWork = LankaConnect.Domain.Common.IUnitOfWork;
using LankaConnect.Domain.Shared.ValueObjects;
using LankaConnect.Domain.Shared.Enums;

namespace LankaConnect.Application.Businesses.Commands.AddService;

public class AddServiceCommandHandler : ICommandHandler<AddServiceCommand, Guid>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly DomainUnitOfWork _unitOfWork;

    public AddServiceCommandHandler(IBusinessRepository businessRepository, DomainUnitOfWork unitOfWork)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(AddServiceCommand request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(request.BusinessId, cancellationToken);
        if (business == null)
            throw new BusinessNotFoundException(request.BusinessId);

        // Create service with USD pricing for US businesses
        var serviceResult = Service.Create(
            request.Name,
            request.Description,
            Money.Create(request.Price, Currency.USD).Value,
            request.Duration,
            request.BusinessId);

        if (!serviceResult.IsSuccess)
            throw new InvalidOperationException(serviceResult.Error);

        var service = serviceResult.Value;
        if (!request.IsAvailable)
            service.Deactivate();

        // Add to business
        var addResult = business.AddService(service);
        if (!addResult.IsSuccess)
            throw new InvalidOperationException(addResult.Error);

        await _unitOfWork.CommitAsync(cancellationToken);

        return Result<Guid>.Success(service.Id);
    }
}