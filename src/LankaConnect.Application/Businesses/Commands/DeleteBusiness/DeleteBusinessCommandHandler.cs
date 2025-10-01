using LankaConnect.Application.Common.Interfaces;
using LankaConnect.Domain.Common;
using LankaConnect.Domain.Common.Exceptions;

namespace LankaConnect.Application.Businesses.Commands.DeleteBusiness;

public class DeleteBusinessCommandHandler : ICommandHandler<DeleteBusinessCommand>
{
    private readonly IBusinessRepository _businessRepository;
    private readonly LankaConnect.Domain.Common.IUnitOfWork _unitOfWork;

    public DeleteBusinessCommandHandler(IBusinessRepository businessRepository, LankaConnect.Domain.Common.IUnitOfWork unitOfWork)
    {
        _businessRepository = businessRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(DeleteBusinessCommand request, CancellationToken cancellationToken)
    {
        var business = await _businessRepository.GetByIdAsync(request.Id, cancellationToken);
        if (business == null)
            throw new BusinessNotFoundException(request.Id);

        await _businessRepository.DeleteAsync(request.Id, cancellationToken);
        await _unitOfWork.CommitAsync(cancellationToken);

        return Result.Success();
    }
}