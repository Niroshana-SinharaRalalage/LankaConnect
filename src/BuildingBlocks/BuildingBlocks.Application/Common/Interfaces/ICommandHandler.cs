using MediatR;
using LankaConnect.Domain.Common;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}