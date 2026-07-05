using System;
using LankaConnect.BuildingBlocks.Domain;
namespace LankaConnect.BuildingBlocks.Application.Common.Models.Security;

public class MultiJurisdictionCompliance : LegacyBaseEntity
{
    public Guid ComplianceId { get; set; } = Guid.NewGuid();
    public List<string> Jurisdictions { get; set; } = new();
    public Dictionary<string, bool> ComplianceStatus { get; set; } = new();
    public DateTime LastAssessment { get; set; } = DateTime.UtcNow;
}