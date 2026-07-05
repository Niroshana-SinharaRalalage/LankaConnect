using LankaConnect.Modules.Communications.Domain.Entities;
using LankaConnect.Modules.Communications.Contracts;
namespace LankaConnect.Modules.Communications.Application.Mappings;

/// <summary>
/// Domain -> Contracts projection extensions for the EmailGroup aggregate.
/// Wave 5.4.b (2026-06-13).
/// </summary>
/// <remarks>
/// Mirrors the FormContractMappings pattern (Wave 5.3b). Lives in
/// Communications.Application because the Contracts layer must not pull
/// Domain (the ArchTest
/// <c>Modules_Communications_Contracts_DependsOnlyOnBuildingBlocksContracts</c>
/// pins that boundary). After W5.4.d.2 the Domain side moves to
/// <c>LankaConnect.Modules.Communications.Domain</c>; this extension's input
/// type updates with it, but the projection logic is stable.
/// </remarks>
public static class EmailGroupContractMappings
{
    public static EmailGroupSummaryDto ToSummaryDto(this EmailGroup source) =>
        new(
            Id: source.Id,
            Name: source.Name,
            Description: source.Description,
            OwnerId: source.OwnerId,
            EmailCount: source.GetEmailCount(),
            IsActive: source.IsActive,
            CreatedAt: source.CreatedAt,
            UpdatedAt: source.UpdatedAt);

    public static EmailGroupDetailDto ToDetailDto(this EmailGroup source) =>
        new(
            Id: source.Id,
            Name: source.Name,
            Description: source.Description,
            OwnerId: source.OwnerId,
            Emails: source.GetEmailList(),
            IsActive: source.IsActive,
            CreatedAt: source.CreatedAt,
            UpdatedAt: source.UpdatedAt);
}
