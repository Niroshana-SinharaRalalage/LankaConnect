using MediatR;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.BuildingBlocks.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Enums;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}