namespace LankaConnect.Modules.Forms.Contracts;

/// <summary>
/// Contracts mirror of <c>LankaConnect.Modules.Forms.Domain.Enums.FormStatus</c>.
/// Duplicated by deliberate design (Wave 5.3a 2026-06-11): the Contracts layer
/// must NOT depend on Forms.Domain (enforced by ArchTest rule
/// <c>Modules_Forms_Contracts_DependsOnlyOnBuildingBlocksContracts</c>).
/// Forms.Application is responsible for the 1:1 mapping between this enum and
/// the Domain enum via <c>FormContractMappings</c>.
/// </summary>
public enum FormStatusDto
{
    Draft = 0,
    Active = 1,
    Closed = 2,
    Archived = 3,
}
