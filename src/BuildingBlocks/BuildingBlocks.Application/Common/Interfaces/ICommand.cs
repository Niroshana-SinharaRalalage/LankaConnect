using MediatR;
using LankaConnect.BuildingBlocks.Domain;
using LankaConnect.Domain.Business;
using LankaConnect.Domain.Enterprise;
using LankaConnect.BuildingBlocks.Domain.Models;
using LankaConnect.BuildingBlocks.Domain.Monitoring;
using LankaConnect.BuildingBlocks.Domain.ValueObjects;
using LankaConnect.BuildingBlocks.Domain.Security;
using LankaConnect.BuildingBlocks.Domain.Recovery;
using LankaConnect.BuildingBlocks.Domain.Database;
using LankaConnect.BuildingBlocks.Domain.Enums;
using MultiLanguageModels = LankaConnect.BuildingBlocks.Domain.Database.MultiLanguageRoutingModels;
namespace LankaConnect.BuildingBlocks.Application.Common.Interfaces;

public interface ICommand : IRequest<Result>
{
}

public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}